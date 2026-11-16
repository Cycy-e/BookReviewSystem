namespace BookReviewSystem.Models;

public class Review
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public int BookId { get; set; }
    public string Content { get; set; }
    public string CreatedAt { get; set; }

    // Joined from Users and Books
    public string UserName { get; set; }
    public string BookTitle { get; set; }
}
