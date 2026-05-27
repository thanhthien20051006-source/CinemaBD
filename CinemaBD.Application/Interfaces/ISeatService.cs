using CinemaBD.Domain.Entities;

namespace CinemaBD.Application.Interfaces;

public interface ISeatService
{
    Task<bool> ShowtimeExistsAsync(string showtimeId, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<Seat>> GetSeatsByShowtimeAsync(string showtimeId, CancellationToken cancellationToken = default);
}
