using CinemaBD.Domain.Entities;

namespace CinemaBD.Application.Interfaces;

public interface IAdminCinemaService
{
    Task<List<Cinema>> GetAllAsync(CancellationToken ct = default);
    Task<Cinema?> GetByIdAsync(string id, CancellationToken ct = default);
    Task<Cinema> UpsertAsync(string? id, string name, string? address, string? phone, string? status, CancellationToken ct = default);
    Task<bool> ToggleStatusAsync(string id, CancellationToken ct = default);
}
