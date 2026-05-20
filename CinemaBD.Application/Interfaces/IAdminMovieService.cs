using CinemaBD.Domain.Entities;

namespace CinemaBD.Application.Interfaces;

public interface IAdminMovieService
{
    Task<IReadOnlyCollection<Movie>> GetAllAsync(string? search, CancellationToken cancellationToken = default);
    Task<Movie?> GetByIdAsync(string id, CancellationToken cancellationToken = default);
    Task<Movie> CreateAsync(Movie movie, CancellationToken cancellationToken = default);
    Task<Movie?> UpdateAsync(string id, Movie movie, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(string id, CancellationToken cancellationToken = default);
}
