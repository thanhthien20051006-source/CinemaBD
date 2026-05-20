using CinemaBD.Web.Models;

namespace CinemaBD.Web.Core;

public interface IAdminLegacyReadService
{
    Task<AdminListPageViewModel> GetServicesAsync(CancellationToken cancellationToken = default);
    Task<AdminListPageViewModel> GetSeatsAsync(CancellationToken cancellationToken = default);
    Task<AdminListPageViewModel> GetRoomsAsync(CancellationToken cancellationToken = default);
    Task<AdminListPageViewModel> GetGenresAsync(CancellationToken cancellationToken = default);
    Task<AdminListPageViewModel> GetArticlesAsync(CancellationToken cancellationToken = default);
    Task<AdminListPageViewModel> GetEventsAsync(CancellationToken cancellationToken = default);
    Task<AdminListPageViewModel> GetEmployeesAsync(CancellationToken cancellationToken = default);
    Task<AdminListPageViewModel> GetRolesAsync(CancellationToken cancellationToken = default);
    Task<AdminListPageViewModel> GetRolePermissionsAsync(CancellationToken cancellationToken = default);
    Task<AdminListPageViewModel> GetInvoicesAsync(CancellationToken cancellationToken = default);
    Task<AdminThongKeViewModel> GetStatisticsAsync(CancellationToken cancellationToken = default);
}
