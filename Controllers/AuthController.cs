using System.Text.RegularExpressions;
using BookReviewSystem.Data;
using BookReviewSystem.Models;

namespace BookReviewSystem.Controllers;

/// <summary>FR-1, FR-2, FR-3: registration, login and logout.</summary>
public class AuthController
{
    private static readonly Regex EmailPattern = new(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", RegexOptions.Compiled);

    public OperationResult Register(string fullName, string email, string password)
    {
        fullName = (fullName ?? "").Trim();
        email = (email ?? "").Trim();
        password = password ?? "";

        if (fullName.Length == 0 || email.Length == 0 || password.Length == 0)
            return OperationResult.Fail("Please fill in the full name, email and password.");
        if (!EmailPattern.IsMatch(email))
            return OperationResult.Fail("The email address is not in a valid format (example: name@mail.com).");
        if (password.Length < 8 || !password.Any(char.IsLetter) || !password.Any(char.IsDigit))
            return OperationResult.Fail("The password is too weak. Use at least 8 characters with letters and numbers.");

        using var conn = Database.Open();
        using (var check = Database.Cmd(conn, "SELECT COUNT(*) FROM Users WHERE LOWER(Email) = LOWER(@e)", ("@e", email)))
        {
            if ((long)check.ExecuteScalar() > 0)
                return OperationResult.Fail("An account with this email already exists.");
        }

        using var insert = Database.Cmd(conn,
            "INSERT INTO Users (FullName, Email, PasswordHash, Role) VALUES (@n, @e, @p, @r)",
            ("@n", fullName), ("@e", email), ("@p", PasswordHasher.Hash(password)),
            ("@r", User.ReaderRole));
        insert.ExecuteNonQuery();
        return OperationResult.Ok("Account created. You can now log in.");
    }

    public OperationResult Login(string email, string password)
    {
        email = (email ?? "").Trim();
        password = password ?? "";
        if (email.Length == 0 || password.Length == 0)
            return OperationResult.Fail("Please enter your email and password.");

        using var conn = Database.Open();
        using var cmd = Database.Cmd(conn,
            "SELECT Id, FullName, Email, PasswordHash, Role, IsActive, " + Database.Stamp("CreatedAt") + " FROM Users WHERE LOWER(Email) = LOWER(@e)", ("@e", email));
        using var reader = cmd.ExecuteReader();

        if (!reader.Read())
            return OperationResult.Fail("No account was found with that email address.");
        if (!PasswordHasher.Verify(password, reader.GetString(3)))
            return OperationResult.Fail("The password is incorrect.");
        if (!reader.GetBoolean(5))
            return OperationResult.Fail("This account has been deactivated. Please contact an administrator.");

        Session.SignIn(new User
        {
            Id = reader.GetInt32(0),
            FullName = reader.GetString(1),
            Email = reader.GetString(2),
            Role = reader.GetString(4),
            IsActive = true,
            CreatedAt = reader.GetString(6)
        });
        return OperationResult.Ok();
    }

    public void Logout() => Session.Clear();
}
