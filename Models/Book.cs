namespace BookReviewSystem.Models;

public class Book
{
    public int Id { get; set; }
    public string Title { get; set; }
    public string Author { get; set; }
    public string Category { get; set; }
    public string Isbn { get; set; }
    public int PublicationYear { get; set; }
    public string Description { get; set; }

    // Calculated from the Ratings and Reviews tables
    public double AverageRating { get; set; }
    public int RatingCount { get; set; }
    public int ReviewCount { get; set; }
}
