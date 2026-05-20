using CinemaBD.Domain.Entities;

namespace CinemaBD.Application.Interfaces;

public interface ISeatService
{
    Task<IReadOnlyCollection<Seat>> GetSeatsByShowtimeAsync(string showtimeId, CancellationToken cancellationToken = default);
}
