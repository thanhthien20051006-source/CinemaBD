namespace CinemaBD.Web.Models;

public class ArticleDetailsPageViewModel
{
    public ArticleViewModel Article { get; set; } = new();
    public IReadOnlyList<ArticleViewModel> LatestArticles { get; set; } = Array.Empty<ArticleViewModel>();
}
