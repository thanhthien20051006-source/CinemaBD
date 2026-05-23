using CinemaBD.Domain.Entities;

namespace CinemaBD.Application.Interfaces;

public interface IAdminRoomService
{
    Task<List<Room>> GetAllAsync(string? cinemaId = null, CancellationToken ct = default);
    Task<Room?> GetByIdAsync(string id, CancellationToken ct = default);
    Task<Room> UpsertAsync(string? id, string name, int seatCount, string? status, CancellationToken ct = default);
    Task<bool> ToggleStatusAsync(string id, CancellationToken ct = default);
}
