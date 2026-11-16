using BookReviewSystem.Data;
using BookReviewSystem.Models;

namespace BookReviewSystem.Controllers;

/// <summary>FR-9 to FR-12 and FR-15: reviews and ratings.</summary>
public class ReviewController
{
    public const int MaxReviewLength = 1000;

    private const string ReviewSelect = @"
        SELECT rv.Id, rv.UserId, rv.BookId, rv.Content, rv.CreatedAt, u.FullName, b.Title
        FROM Reviews rv
        JOIN Users u ON u.Id = rv.UserId
        JOIN Books b ON b.Id = rv.BookId ";

    private static Review Read(Microsoft.Data.Sqlite.SqliteDataReader r) => new()
    {
        Id = r.GetInt32(0), UserId = r.GetInt32(1), BookId = r.GetInt32(2),
        Content = r.GetString(3), CreatedAt = r.GetString(4),
        UserName = r.GetString(5), BookTitle = r.GetString(6)
    };

    /// <summary>All reviews for a book, newest first.</summary>
    public List<Review> GetForBook(int bookId)
    {
        var list = new List<Review>();
        using var conn = Database.Open();
        using var cmd = Database.Cmd(conn, ReviewSelect + "WHERE rv.BookId = @b ORDER BY rv.CreatedAt DESC, rv.Id DESC", ("@b", bookId));
        using var reader = cmd.ExecuteReader();
        while (reader.Read()) list.Add(Read(reader));
        return list;
    }

    /// <summary>Every review in the system (administrators only).</summary>
    public List<Review> GetAll()
    {
        var list = new List<Review>();
        if (!Session.IsAdmin) return list;
        using var conn = Database.Open();
        using var cmd = Database.Cmd(conn, ReviewSelect + "ORDER BY rv.CreatedAt DESC, rv.Id DESC");
        using var reader = cmd.ExecuteReader();
        while (reader.Read()) list.Add(Read(reader));
        return list;
    }

    public OperationResult Add(int bookId, string content)
    {
        if (!Session.IsReader) return OperationResult.Fail("Please log in as a reader to write a review.");
        var error = ValidateContent(content);
        if (error != null) return OperationResult.Fail(error);

        using var conn = Database.Open();
        using var cmd = Database.Cmd(conn,
            "INSERT INTO Reviews (UserId, BookId, Content, CreatedAt) VALUES (@u, @b, @c, @d)",
            ("@u", Session.CurrentUser.Id), ("@b", bookId), ("@c", content.Trim()), ("@d", Database.Now()));
        cmd.ExecuteNonQuery();
        return OperationResult.Ok("Review posted.");
    }

    public OperationResult Update(int reviewId, string content)
    {
        if (!Session.IsReader) return OperationResult.Fail("Please log in as a reader.");
        var error = ValidateContent(content);
        if (error != null) return OperationResult.Fail(error);

        using var conn = Database.Open();
        using var cmd = Database.Cmd(conn,
            "UPDATE Reviews SET Content = @c WHERE Id = @id AND UserId = @u",
            ("@c", content.Trim()), ("@id", reviewId), ("@u", Session.CurrentUser.Id));
        return cmd.ExecuteNonQuery() > 0
            ? OperationResult.Ok("Review updated.")
            : OperationResult.Fail("You can only edit your own reviews.");
    }

    public OperationResult Delete(int reviewId)
    {
        if (!Session.IsReader) return OperationResult.Fail("Please log in as a reader.");
        using var conn = Database.Open();
        using var cmd = Database.Cmd(conn, "DELETE FROM Reviews WHERE Id = @id AND UserId = @u",
            ("@id", reviewId), ("@u", Session.CurrentUser.Id));
        return cmd.ExecuteNonQuery() > 0
            ? OperationResult.Ok("Review deleted.")
            : OperationResult.Fail("You can only delete your own reviews.");
    }

    /// <summary>Administrators can remove any review (FR-15).</summary>
    public OperationResult AdminDelete(int reviewId)
    {
        if (!Session.IsAdmin) return OperationResult.Fail("Only administrators can remove other people's reviews.");
        using var conn = Database.Open();
        using var cmd = Database.Cmd(conn, "DELETE FROM Reviews WHERE Id = @id", ("@id", reviewId));
        cmd.ExecuteNonQuery();
        return OperationResult.Ok("Review removed.");
    }

    // ---- Ratings (one per reader per book) ----

    public int? GetMyRating(int bookId)
    {
        if (!Session.IsLoggedIn) return null;
        using var conn = Database.Open();
        using var cmd = Database.Cmd(conn, "SELECT Stars FROM Ratings WHERE UserId = @u AND BookId = @b",
            ("@u", Session.CurrentUser.Id), ("@b", bookId));
        var value = cmd.ExecuteScalar();
        return value == null ? null : Convert.ToInt32(value);
    }

    /// <summary>Adds the rating, or changes it if the reader already rated this book.</summary>
    public OperationResult SetRating(int bookId, int stars)
    {
        if (!Session.IsReader) return OperationResult.Fail("Please log in as a reader to rate a book.");
        if (stars < 1 || stars > 5) return OperationResult.Fail("A rating must be between 1 and 5 stars.");

        using var conn = Database.Open();
        using var cmd = Database.Cmd(conn,
            @"INSERT INTO Ratings (UserId, BookId, Stars) VALUES (@u, @b, @s)
              ON CONFLICT (UserId, BookId) DO UPDATE SET Stars = excluded.Stars",
            ("@u", Session.CurrentUser.Id), ("@b", bookId), ("@s", stars));
        cmd.ExecuteNonQuery();
        return OperationResult.Ok("Rating saved.");
    }

    public OperationResult RemoveRating(int bookId)
    {
        if (!Session.IsReader) return OperationResult.Fail("Please log in as a reader.");
        using var conn = Database.Open();
        using var cmd = Database.Cmd(conn, "DELETE FROM Ratings WHERE UserId = @u AND BookId = @b",
            ("@u", Session.CurrentUser.Id), ("@b", bookId));
        return cmd.ExecuteNonQuery() > 0
            ? OperationResult.Ok("Rating removed.")
            : OperationResult.Fail("You have not rated this book yet.");
    }

    private static string ValidateContent(string content)
    {
        if (string.IsNullOrWhiteSpace(content)) return "A review cannot be empty.";
        if (content.Trim().Length > MaxReviewLength)
            return $"A review can have at most {MaxReviewLength} characters.";
        return null;
    }
}
