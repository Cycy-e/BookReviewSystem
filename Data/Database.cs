using Microsoft.Data.Sqlite;
using BookReviewSystem.Models;

namespace BookReviewSystem.Data;

/// <summary>
/// SQLite database access: creates the schema on first run, seeds an administrator
/// and a few sample books, and hands out open connections.
/// </summary>
public static class Database
{
    public const string DefaultAdminEmail = "admin@bookreview.com";
    public const string DefaultAdminPassword = "Admin@123";

    public static string DbPath { get; } = Path.Combine(AppContext.BaseDirectory, "bookreview.db");

    public static SqliteConnection Open()
    {
        var conn = new SqliteConnection($"Data Source={DbPath}");
        conn.Open();
        using var pragma = conn.CreateCommand();
        pragma.CommandText = "PRAGMA foreign_keys = ON;";
        pragma.ExecuteNonQuery();
        return conn;
    }

    /// <summary>Builds a command with named parameters.</summary>
    public static SqliteCommand Cmd(SqliteConnection conn, string sql, params (string Name, object Value)[] args)
    {
        var cmd = conn.CreateCommand();
        cmd.CommandText = sql;
        foreach (var (name, value) in args)
            cmd.Parameters.AddWithValue(name, value ?? DBNull.Value);
        return cmd;
    }

    public static string Now() => DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

    public static void Initialize()
    {
        using var conn = Open();

        const string schema = @"
            CREATE TABLE IF NOT EXISTS Users (
                Id           INTEGER PRIMARY KEY AUTOINCREMENT,
                FullName     TEXT NOT NULL,
                Email        TEXT NOT NULL UNIQUE COLLATE NOCASE,
                PasswordHash TEXT NOT NULL,
                Role         TEXT NOT NULL CHECK (Role IN ('Reader','Administrator')),
                IsActive     INTEGER NOT NULL DEFAULT 1,
                CreatedAt    TEXT NOT NULL
            );
            CREATE TABLE IF NOT EXISTS Books (
                Id              INTEGER PRIMARY KEY AUTOINCREMENT,
                Title           TEXT NOT NULL,
                Author          TEXT NOT NULL,
                Category        TEXT NOT NULL,
                Isbn            TEXT NOT NULL UNIQUE,
                PublicationYear INTEGER NOT NULL,
                Description     TEXT
            );
            CREATE TABLE IF NOT EXISTS Ratings (
                Id     INTEGER PRIMARY KEY AUTOINCREMENT,
                UserId INTEGER NOT NULL REFERENCES Users(Id) ON DELETE CASCADE,
                BookId INTEGER NOT NULL REFERENCES Books(Id) ON DELETE CASCADE,
                Stars  INTEGER NOT NULL CHECK (Stars BETWEEN 1 AND 5),
                UNIQUE (UserId, BookId)
            );
            CREATE TABLE IF NOT EXISTS Reviews (
                Id        INTEGER PRIMARY KEY AUTOINCREMENT,
                UserId    INTEGER NOT NULL REFERENCES Users(Id) ON DELETE CASCADE,
                BookId    INTEGER NOT NULL REFERENCES Books(Id) ON DELETE CASCADE,
                Content   TEXT NOT NULL,
                CreatedAt TEXT NOT NULL
            );";
        using (var cmd = Cmd(conn, schema)) cmd.ExecuteNonQuery();

        Seed(conn);
    }

    private static void Seed(Microsoft.Data.Sqlite.SqliteConnection conn)
    {
        long users = (long)Cmd(conn, "SELECT COUNT(*) FROM Users").ExecuteScalar();
        if (users == 0)
        {
            using var cmd = Cmd(conn,
                "INSERT INTO Users (FullName, Email, PasswordHash, Role, IsActive, CreatedAt) VALUES (@n, @e, @p, @r, 1, @d)",
                ("@n", "System Administrator"), ("@e", DefaultAdminEmail),
                ("@p", PasswordHasher.Hash(DefaultAdminPassword)), ("@r", User.AdminRole), ("@d", Now()));
            cmd.ExecuteNonQuery();
        }

        long books = (long)Cmd(conn, "SELECT COUNT(*) FROM Books").ExecuteScalar();
        if (books == 0)
        {
            var samples = new (string Title, string Author, string Category, string Isbn, int Year, string Desc)[]
            {
                ("Things Fall Apart", "Chinua Achebe", "Fiction", "9780385474542", 1958, "A novel about a proud Igbo leader and the arrival of colonial rule in his village."),
                ("Half of a Yellow Sun", "Chimamanda Ngozi Adichie", "Fiction", "9781400095209", 2006, "Lives intertwine during the Nigerian civil war."),
                ("The Alchemist", "Paulo Coelho", "Fiction", "9780062315007", 1988, "A shepherd boy travels in search of a treasure and his personal legend."),
                ("Clean Code", "Robert C. Martin", "Programming", "9780132350884", 2008, "Practical advice on writing readable and maintainable code."),
                ("Atomic Habits", "James Clear", "Self-help", "9780735211292", 2018, "How small daily changes build lasting habits."),
                ("Sapiens", "Yuval Noah Harari", "History", "9780062316097", 2011, "A short history of humankind from the Stone Age to today.")
            };
            foreach (var b in samples)
            {
                using var cmd = Cmd(conn,
                    "INSERT INTO Books (Title, Author, Category, Isbn, PublicationYear, Description) VALUES (@t, @a, @c, @i, @y, @d)",
                    ("@t", b.Title), ("@a", b.Author), ("@c", b.Category), ("@i", b.Isbn), ("@y", b.Year), ("@d", b.Desc));
                cmd.ExecuteNonQuery();
            }
        }
    }
}
