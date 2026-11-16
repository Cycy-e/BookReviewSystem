using BookReviewSystem.Data;
using BookReviewSystem.Models;

namespace BookReviewSystem.Controllers;

/// <summary>FR-16: administrators view users and deactivate or reactivate accounts.</summary>
public class AdminController
{
    public List<User> GetUsers()
    {
        var list = new List<User>();
        if (!Session.IsAdmin) return list;

        using var conn = Database.Open();
        using var cmd = Database.Cmd(conn,
            "SELECT Id, FullName, Email, Role, IsActive, CreatedAt FROM Users ORDER BY FullName COLLATE NOCASE");
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            list.Add(new User
            {
                Id = reader.GetInt32(0), FullName = reader.GetString(1), Email = reader.GetString(2),
                Role = reader.GetString(3), IsActive = reader.GetInt32(4) == 1, CreatedAt = reader.GetString(5)
            });
        }
        return list;
    }

    public OperationResult SetActive(int userId, bool active)
    {
        if (!Session.IsAdmin) return OperationResult.Fail("Only administrators can manage users.");
        if (userId == Session.CurrentUser.Id)
            return OperationResult.Fail("You cannot deactivate your own account.");

        using var conn = Database.Open();
        using var cmd = Database.Cmd(conn, "UPDATE Users SET IsActive = @a WHERE Id = @id",
            ("@a", active ? 1 : 0), ("@id", userId));
        cmd.ExecuteNonQuery();
        return OperationResult.Ok(active ? "Account reactivated." : "Account deactivated.");
    }
}
