namespace CinemaBD.Web.Models;

public class HomeIndexViewModel
{
    public IReadOnlyList<MovieViewModel> ShowingMovies { get; set; } = Array.Empty<MovieViewModel>();
    public IReadOnlyList<MovieViewModel> UpcomingMovies { get; set; } = Array.Empty<MovieViewModel>();
    public int ShowingPage { get; set; } = 1;
    public int UpcomingPage { get; set; } = 1;
    public int PageSize { get; set; } = 4;
    public int ShowingTotalPages { get; set; } = 1;
    public int UpcomingTotalPages { get; set; } = 1;
}
