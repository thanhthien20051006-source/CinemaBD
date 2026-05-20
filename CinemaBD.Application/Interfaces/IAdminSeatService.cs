using CinemaBD.Domain.Entities;

namespace CinemaBD.Application.Interfaces;

public interface IAdminSeatService
{
    Task<List<Seat>> GetAllAsync(string? roomId = null, string? search = null, CancellationToken ct = default);
    Task<List<Seat>> GetSeatMapAsync(string roomId, CancellationToken ct = default);
    Task<string?> ToggleStatusAsync(string seatId, CancellationToken ct = default);
}
