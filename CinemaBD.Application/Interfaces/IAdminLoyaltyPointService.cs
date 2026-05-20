using CinemaBD.Domain.Entities;

namespace CinemaBD.Application.Interfaces;

public interface IAdminLoyaltyPointService
{
    Task<List<LoyaltyPoint>> GetAllAsync(string? search = null, CancellationToken ct = default);
    Task<LoyaltyPoint?> GetByCustomerIdAsync(string customerId, CancellationToken ct = default);
    Task<LoyaltyPoint> AdjustAsync(string customerId, int points, string action, CancellationToken ct = default);
}
