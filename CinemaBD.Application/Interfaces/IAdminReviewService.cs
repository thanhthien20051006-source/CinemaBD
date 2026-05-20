using CinemaBD.Domain.Entities;

namespace CinemaBD.Application.Interfaces;

public interface IAdminReviewService
{
    Task<List<Review>> GetAllAsync(CancellationToken ct = default);
    Task<bool> ToggleHiddenAsync(int id, CancellationToken ct = default);
    Task<bool> DeleteAsync(int id, CancellationToken ct = default);
}
