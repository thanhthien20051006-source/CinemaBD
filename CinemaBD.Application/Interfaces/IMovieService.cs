using CinemaBD.Domain.Entities;

namespace CinemaBD.Application.Interfaces;

public interface IMovieService
{
    Task<IReadOnlyCollection<Movie>> GetNowShowingAsync(CancellationToken cancellationToken = default);
    Task<Movie?> GetByIdAsync(string id, CancellationToken cancellationToken = default);
}
