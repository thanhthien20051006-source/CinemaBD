namespace CinemaBD.Web.Models;

public class AdminReviewViewModel
{
    public int Id { get; set; }
    public string MovieId { get; set; } = string.Empty;
    public string CustomerId { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public int Rating { get; set; } = 5;
    public bool IsHidden { get; set; }
    public DateTime CreatedAt { get; set; }
    public string? MovieTitle { get; set; }
    public string? CustomerName { get; set; }
}
