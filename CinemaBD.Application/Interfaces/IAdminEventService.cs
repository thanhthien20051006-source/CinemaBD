using CinemaBD.Domain.Entities;

namespace CinemaBD.Application.Interfaces;

public interface IAdminEventService
{
    Task<List<Event>> GetAllAsync(CancellationToken ct = default);
    Task<Event?> GetByIdAsync(int id, CancellationToken ct = default);
    Task<Event> CreateAsync(string title, string? description, string? imageUrl, DateTime? startDate, DateTime? endDate, CancellationToken ct = default);
    Task<Event> UpdateAsync(int id, string title, string? description, string? imageUrl, DateTime? startDate, DateTime? endDate, CancellationToken ct = default);
    Task<bool> DeleteAsync(int id, CancellationToken ct = default);
}
