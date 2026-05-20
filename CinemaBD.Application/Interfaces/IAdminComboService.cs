using CinemaBD.Domain.Entities;

namespace CinemaBD.Application.Interfaces;

public interface IAdminComboService
{
    Task<List<Combo>> GetAllAsync(CancellationToken ct = default);
    Task<Combo?> GetByIdAsync(string id, CancellationToken ct = default);
    Task<Combo> UpsertAsync(string id, string name, decimal price, string? description, string? imageUrl, CancellationToken ct = default);
    Task<bool> DeleteAsync(string id, CancellationToken ct = default);
}
