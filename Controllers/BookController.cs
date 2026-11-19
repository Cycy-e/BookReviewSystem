using Npgsql;
using BookReviewSystem.Data;
using BookReviewSystem.Models;

namespace BookReviewSystem.Controllers;

/// <summary>FR-5 to FR-8, FR-13, FR-14: book catalogue, search, filter and sort.</summary>
public class BookController
{
    public const string SortTitle = "Title";
    public const string SortYear = "Publication year";
    public const string SortRating = "Average rating";

    private const string BookSelect = @"
        SELECT b.Id, b.Title, b.Author, b.Category, b.Isbn, b.PublicationYear, COALESCE(b.Description, ''),
               COALESCE(r.Avg, 0), COALESCE(r.Cnt, 0),
               (SELECT CAST(COUNT(*) AS integer) FROM Reviews rv WHERE rv.BookId = b.Id)
        FROM Books b
        LEFT JOIN (SELECT BookId, CAST(AVG(Stars) AS double precision) AS Avg, CAST(COUNT(*) AS integer) AS Cnt FROM Ratings GROUP BY BookId) r
               ON r.BookId = b.Id ";

    private static Book Read(NpgsqlDataReader r) => new()
    {
        Id = r.GetInt32(0),
        Title = r.GetString(1),
        Author = r.GetString(2),
        Category = r.GetString(3),
        Isbn = r.GetString(4),
        PublicationYear = r.GetInt32(5),
        Description = r.GetString(6),
        AverageRating = r.GetDouble(7),
        RatingCount = r.GetInt32(8),
        ReviewCount = r.GetInt32(9)
    };

    /// <summary>Search by title, author, category or ISBN, with optional filters and sorting.</summary>
    public List<Book> Search(string query, string category, int minRating, string sortBy)
    {
        query = (query ?? "").Trim();
        string orderBy = sortBy switch
        {
            SortYear => "b.PublicationYear DESC, b.Title",
            SortRating => "COALESCE(r.Avg, 0) DESC, b.Title",
            _ => "LOWER(b.Title)"
        };

        string sql = BookSelect + @"
            WHERE (@q = '' OR b.Title ILIKE @like OR b.Author ILIKE @like OR b.Category ILIKE @like OR b.Isbn ILIKE @like)
              AND (@cat = '' OR b.Category = @cat)
              AND COALESCE(r.Avg, 0) >= @min
            ORDER BY " + orderBy;

        var list = new List<Book>();
        using var conn = Database.Open();
        using var cmd = Database.Cmd(conn, sql,
            ("@q", query), ("@like", "%" + query + "%"),
            ("@cat", category ?? ""), ("@min", minRating));
        using var reader = cmd.ExecuteReader();
        while (reader.Read()) list.Add(Read(reader));
        return list;
    }

    public List<string> GetCategories()
    {
        var list = new List<string>();
        using var conn = Database.Open();
        using var cmd = Database.Cmd(conn, "SELECT Category FROM Books GROUP BY Category ORDER BY LOWER(Category)");
        using var reader = cmd.ExecuteReader();
        while (reader.Read()) list.Add(reader.GetString(0));
        return list;
    }

    public Book GetById(int id)
    {
        using var conn = Database.Open();
        using var cmd = Database.Cmd(conn, BookSelect + "WHERE b.Id = @id", ("@id", id));
        using var reader = cmd.ExecuteReader();
        return reader.Read() ? Read(reader) : null;
    }

    public OperationResult Add(Book book)
    {
        if (!Session.IsAdmin) return OperationResult.Fail("Only administrators can add books.");
        var error = Validate(book);
        if (error != null) return OperationResult.Fail(error);

        using var conn = Database.Open();
        if (IsbnExists(conn, book.Isbn, 0))
            return OperationResult.Fail("A book with this ISBN already exists.");

        using var cmd = Database.Cmd(conn,
            "INSERT INTO Books (Title, Author, Category, Isbn, PublicationYear, Description) VALUES (@t, @a, @c, @i, @y, @d)",
            ("@t", book.Title.Trim()), ("@a", book.Author.Trim()), ("@c", book.Category.Trim()),
            ("@i", book.Isbn.Trim()), ("@y", book.PublicationYear), ("@d", (book.Description ?? "").Trim()));
        cmd.ExecuteNonQuery();
        return OperationResult.Ok("Book added.");
    }

    public OperationResult Update(Book book)
    {
        if (!Session.IsAdmin) return OperationResult.Fail("Only administrators can edit books.");
        var error = Validate(book);
        if (error != null) return OperationResult.Fail(error);

        using var conn = Database.Open();
        if (IsbnExists(conn, book.Isbn, book.Id))
            return OperationResult.Fail("Another book already uses this ISBN.");

        using var cmd = Database.Cmd(conn,
            @"UPDATE Books SET Title = @t, Author = @a, Category = @c, Isbn = @i,
                               PublicationYear = @y, Description = @d WHERE Id = @id",
            ("@t", book.Title.Trim()), ("@a", book.Author.Trim()), ("@c", book.Category.Trim()),
            ("@i", book.Isbn.Trim()), ("@y", book.PublicationYear), ("@d", (book.Description ?? "").Trim()),
            ("@id", book.Id));
        cmd.ExecuteNonQuery();
        return OperationResult.Ok("Book updated.");
    }

    /// <summary>Deleting a book also deletes its reviews and ratings (ON DELETE CASCADE).</summary>
    public OperationResult Delete(int bookId)
    {
        if (!Session.IsAdmin) return OperationResult.Fail("Only administrators can delete books.");
        using var conn = Database.Open();
        using var cmd = Database.Cmd(conn, "DELETE FROM Books WHERE Id = @id", ("@id", bookId));
        cmd.ExecuteNonQuery();
        return OperationResult.Ok("Book deleted.");
    }

    private static string Validate(Book b)
    {
        if (string.IsNullOrWhiteSpace(b.Title) || string.IsNullOrWhiteSpace(b.Author) ||
            string.IsNullOrWhiteSpace(b.Category) || string.IsNullOrWhiteSpace(b.Isbn))
            return "Title, author, category and ISBN are required.";
        if (b.PublicationYear < 1000 || b.PublicationYear > DateTime.Now.Year + 1)
            return "Please enter a valid publication year.";
        return null;
    }

    private static bool IsbnExists(NpgsqlConnection conn, string isbn, int exceptId)
    {
        using var cmd = Database.Cmd(conn, "SELECT COUNT(*) FROM Books WHERE Isbn = @i AND Id <> @id",
            ("@i", isbn.Trim()), ("@id", exceptId));
        return (long)cmd.ExecuteScalar() > 0;
    }
}
