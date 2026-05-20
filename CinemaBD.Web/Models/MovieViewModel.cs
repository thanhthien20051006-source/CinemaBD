namespace CinemaBD.Web.Models;

public class MovieViewModel
{
    public string Id { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Genre { get; set; } = string.Empty;
    public int DurationMinutes { get; set; }
    public string Director { get; set; } = string.Empty;
    public string Cast { get; set; } = string.Empty;
    public string Country { get; set; } = string.Empty;
    public int? AgeRestriction { get; set; }
    public string Description { get; set; } = string.Empty;
    public string PosterUrl { get; set; } = string.Empty;
    public string TrailerUrl { get; set; } = string.Empty;
    public DateTime? ReleaseDate { get; set; }
    public DateTime? EndDate { get; set; }
    public string Status { get; set; } = string.Empty;
}
