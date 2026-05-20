namespace CinemaBD.Api.Contracts.Movies;

public record MovieResponse(
    string Id,
    string Title,
    string? Genre,
    int DurationMinutes,
    string? Director,
    string? Cast,
    string? Country,
    int? AgeRestriction,
    string? Description,
    string? PosterUrl,
    string? TrailerUrl,
    DateTime? ReleaseDate,
    DateTime? EndDate,
    string? Status
);
