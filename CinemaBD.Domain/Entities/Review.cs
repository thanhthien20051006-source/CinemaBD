namespace CinemaBD.Domain.Entities;

public class Review
{
    public int Id { get; set; }
    public string MovieId { get; set; } = default!;
    public string CustomerId { get; set; } = default!;
    public string Content { get; set; } = default!;
    public int Rating { get; set; } = 5;
    public bool IsHidden { get; set; }
    public bool CanReview { get; set; }
    public string? ReviewRuleMessage { get; set; }
    public DateTime CreatedAt { get; set; }
    public string? MovieTitle { get; set; }
    public string? CustomerName { get; set; }
}
