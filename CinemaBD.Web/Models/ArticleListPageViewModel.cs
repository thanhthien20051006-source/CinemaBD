namespace CinemaBD.Web.Models;

public class ArticleListPageViewModel
{
    public IReadOnlyList<ArticleViewModel> Articles { get; set; } = Array.Empty<ArticleViewModel>();
    public IReadOnlyList<ArticleViewModel> LatestArticles { get; set; } = Array.Empty<ArticleViewModel>();
    public int Page { get; set; }
    public int TotalPages { get; set; }
}
