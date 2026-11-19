using Npgsql;
using BookReviewSystem.Models;

namespace BookReviewSystem.Data;

/// <summary>
/// PostgreSQL access: creates the schema on first run, seeds an administrator
/// and a few sample books, and hands out open connections.
/// </summary>
public static class Database
{
    public const string DefaultAdminEmail = "admin@bookreview.com";
    public const string DefaultAdminPassword = "Admin@123";

    // >>> Change this if your PostgreSQL uses different details. <<<
    // It matches the Docker command from the README. You can also override it without
    // editing code by setting the environment variable BOOKREVIEW_DB.
    private const string DefaultConnectionString =
        "Host=localhost;Port=5432;Database=bookreviewdb;Username=postgres;Password=YOUR_PASSWORD";

    public static string ConnectionString { get; } =
        Environment.GetEnvironmentVariable("BOOKREVIEW_DB") ?? DefaultConnectionString;

    public static NpgsqlConnection Open()
    {
        var conn = new NpgsqlConnection(ConnectionString);
        conn.Open();
        return conn;
    }

    /// <summary>Builds a command with named parameters.</summary>
    public static NpgsqlCommand Cmd(NpgsqlConnection conn, string sql, params (string Name, object Value)[] args)
    {
        var cmd = new NpgsqlCommand(sql, conn);
        foreach (var (name, value) in args)
            cmd.Parameters.AddWithValue(name, value ?? DBNull.Value);
        return cmd;
    }

    /// <summary>SQL fragment that returns a timestamp column as readable text.</summary>
    public static string Stamp(string column) => $"to_char({column}, 'YYYY-MM-DD HH24:MI:SS')";

    public static void Initialize()
    {
        using var conn = Open();

        const string schema = @"
            CREATE TABLE IF NOT EXISTS Users (
                Id           SERIAL PRIMARY KEY,
                FullName     VARCHAR(150) NOT NULL,
                Email        VARCHAR(255) NOT NULL,
                PasswordHash TEXT NOT NULL,
                Role         VARCHAR(20) NOT NULL CHECK (Role IN ('Reader','Administrator')),
                IsActive     BOOLEAN NOT NULL DEFAULT TRUE,
                CreatedAt    TIMESTAMP NOT NULL DEFAULT NOW()
            );
            CREATE UNIQUE INDEX IF NOT EXISTS ux_users_email ON Users (LOWER(Email));

            CREATE TABLE IF NOT EXISTS Books (
                Id              SERIAL PRIMARY KEY,
                Title           VARCHAR(255) NOT NULL,
                Author          VARCHAR(255) NOT NULL,
                Category        VARCHAR(100) NOT NULL,
                Isbn            VARCHAR(30)  NOT NULL UNIQUE,
                PublicationYear INTEGER NOT NULL,
                Description     TEXT
            );

            CREATE TABLE IF NOT EXISTS Ratings (
                Id     SERIAL PRIMARY KEY,
                UserId INTEGER NOT NULL REFERENCES Users(Id) ON DELETE CASCADE,
                BookId INTEGER NOT NULL REFERENCES Books(Id) ON DELETE CASCADE,
                Stars  INTEGER NOT NULL CHECK (Stars BETWEEN 1 AND 5),
                UNIQUE (UserId, BookId)
            );

            CREATE TABLE IF NOT EXISTS Reviews (
                Id        SERIAL PRIMARY KEY,
                UserId    INTEGER NOT NULL REFERENCES Users(Id) ON DELETE CASCADE,
                BookId    INTEGER NOT NULL REFERENCES Books(Id) ON DELETE CASCADE,
                Content   TEXT NOT NULL,
                CreatedAt TIMESTAMP NOT NULL DEFAULT NOW()
            );";
        using (var cmd = Cmd(conn, schema)) cmd.ExecuteNonQuery();

        Seed(conn);
    }

    private static void Seed(NpgsqlConnection conn)
    {
        long users = (long)Cmd(conn, "SELECT COUNT(*) FROM Users").ExecuteScalar();
        if (users == 0)
        {
            using var cmd = Cmd(conn,
                "INSERT INTO Users (FullName, Email, PasswordHash, Role) VALUES (@n, @e, @p, @r)",
                ("@n", "System Administrator"), ("@e", DefaultAdminEmail),
                ("@p", PasswordHasher.Hash(DefaultAdminPassword)), ("@r", User.AdminRole));
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
