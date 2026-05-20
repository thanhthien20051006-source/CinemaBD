using CinemaBD.Domain.Entities;

namespace CinemaBD.Application.Interfaces;

public interface IAdminGenreService
{
    Task<List<Genre>> GetAllAsync(CancellationToken ct = default);
    Task<Genre?> GetByIdAsync(int id, CancellationToken ct = default);
    Task<Genre> UpsertAsync(int id, string name, string? description, CancellationToken ct = default);
    Task<bool> DeleteAsync(int id, CancellationToken ct = default);
}
