using CinemaBD.Domain.Entities;

namespace CinemaBD.Application.Interfaces;

public interface IAdminStatisticsService
{
    Task<RevenueStatistics> GetRevenueAsync(int? year = null, CancellationToken ct = default);
}
