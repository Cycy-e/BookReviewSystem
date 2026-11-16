namespace BookReviewSystem.Models;

public class User
{
    public const string AdminRole = "Administrator";
    public const string ReaderRole = "Reader";

    public int Id { get; set; }
    public string FullName { get; set; }
    public string Email { get; set; }
    public string Role { get; set; }
    public bool IsActive { get; set; }
    public string CreatedAt { get; set; }

    public bool IsAdmin => Role == AdminRole;
}
