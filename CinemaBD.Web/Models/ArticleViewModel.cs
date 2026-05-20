namespace CinemaBD.Web.Models;

public class ArticleViewModel
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Summary { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string ImageName { get; set; } = string.Empty;
    public DateTime PublishedAt { get; set; }
}
