using CinemaBD.Domain.Entities;

namespace CinemaBD.Application.Interfaces;

public interface IShowtimeService
{
    Task<IReadOnlyCollection<ShowtimeDetail>> GetByMovieAsync(string movieId, DateTime? date, CancellationToken cancellationToken = default);
}
