using CinemaBD.Domain.Entities;

namespace CinemaBD.Application.Interfaces;

public interface IAdminDashboardService
{
    Task<AdminDashboard> GetSummaryAsync(CancellationToken cancellationToken = default);
}
