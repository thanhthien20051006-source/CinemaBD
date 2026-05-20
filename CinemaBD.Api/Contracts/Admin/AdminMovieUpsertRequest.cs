namespace CinemaBD.Api.Contracts.Admin;

public record AdminMovieUpsertRequest(
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
