namespace CinemaBD.Domain.Entities;

public class Movie
{
    public string Id { get; set; } = default!;
    public string Title { get; set; } = default!;
    public string? Genre { get; set; }
    public int DurationMinutes { get; set; }
    public string? Director { get; set; }
    public string? Cast { get; set; }
    public string? Country { get; set; }
    public int? AgeRestriction { get; set; }
    public string? Description { get; set; }
    public string? PosterUrl { get; set; }
    public string? TrailerUrl { get; set; }
    public DateTime? ReleaseDate { get; set; }
    public DateTime? EndDate { get; set; }
    public string? Status { get; set; }
}
