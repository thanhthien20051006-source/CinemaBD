namespace CinemaBD.Domain.Entities;

public class Article
{
    public int Id { get; set; }
    public string Title { get; set; } = default!;
    public string? Summary { get; set; }
    public string? Content { get; set; }
    public string? ImageUrl { get; set; }
    public DateTime PublishedAt { get; set; } = DateTime.Now;
}
