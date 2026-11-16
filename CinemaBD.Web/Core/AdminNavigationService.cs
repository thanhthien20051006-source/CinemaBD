using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using CinemaBD.Domain.Entities;
using CinemaBD.Web.Models;

namespace CinemaBD.Web.Core;

public interface IAdminNavigationService
{
    Task<AdminNavigationAccess> GetAccessAsync(CancellationToken cancellationToken = default);
}

public sealed class AdminNavigationAccess
{
    public bool IsMaster { get; init; }
    public HashSet<string> Permissions { get; init; } = new(StringComparer.OrdinalIgnoreCase);

    public bool Can(params string[] keywords)
    {
        if (IsMaster) return true;
        if (keywords.Length == 0 || Permissions.Count == 0) return false;
        return keywords.Any(keyword => Permissions.Any(permission => AdminPermissionMatcher.Matches(permission, [keyword])));
    }

}

public sealed class AdminNavigationService : IAdminNavigationService
{
    private readonly HttpClient _httpClient;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<AdminNavigationService> _logger;

    public AdminNavigationService(HttpClient httpClient, IHttpContextAccessor httpContextAccessor, ILogger<AdminNavigationService> logger)
    {
        _httpClient = httpClient;
        _httpContextAccessor = httpContextAccessor;
        _logger = logger;
        if (_httpClient.BaseAddress == null) _httpClient.BaseAddress = new Uri("http://localhost:5188/");
    }

    public async Task<AdminNavigationAccess> GetAccessAsync(CancellationToken cancellationToken = default)
    {
        var httpContext = _httpContextAccessor.HttpContext;
        var token = httpContext?.Session.GetString("AdminToken");
        var adminIdValue = httpContext?.Session.GetString("AdminId");
        var hasAdminToken = !string.IsNullOrWhiteSpace(token);
        var hasAdminId = !string.IsNullOrWhiteSpace(adminIdValue);
        if (!hasAdminToken || !int.TryParse(adminIdValue, out var adminId) || adminId <= 0)
        {
            _logger.LogWarning("Admin permission check denied because authenticated role context is missing.");
            LogAuthorizationDenial(httpContext, hasAdminToken, hasAdminId, statusCode: null);
            return new AdminNavigationAccess();
        }

        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, "api/admin/roles/authorization-access");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            using var response = await _httpClient.SendAsync(request, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Admin permission check denied for admin {AdminId}; authorization lookup returned HTTP {StatusCode}.", adminId, (int)response.StatusCode);
                LogAuthorizationDenial(httpContext, hasAdminToken, hasAdminId, (int)response.StatusCode);
                return new AdminNavigationAccess();
            }

            var payload = await response.Content.ReadFromJsonAsync<ApiResponse<JsonElement>>(cancellationToken: cancellationToken);
            if (payload is not { Success: true } || payload.Data.ValueKind is JsonValueKind.Undefined or JsonValueKind.Null)
            {
                _logger.LogWarning("Admin permission check denied for admin {AdminId}; authorization response was invalid.", adminId);
                LogAuthorizationDenial(httpContext, hasAdminToken, hasAdminId, (int)response.StatusCode);
                return new AdminNavigationAccess();
            }

            var access = JsonSerializer.Deserialize<AdminAuthorizationAccessResponse>(payload.Data.GetRawText(), new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            if (access is null || access.RoleId <= 0 || access.Permissions is null)
            {
                _logger.LogWarning("Admin permission check denied for admin {AdminId}; authorization response was invalid.", adminId);
                LogAuthorizationDenial(httpContext, hasAdminToken, hasAdminId, (int)response.StatusCode);
                return new AdminNavigationAccess();
            }

            return new AdminNavigationAccess
            {
                IsMaster = access.IsMaster,
                Permissions = access.Permissions.ToHashSet(StringComparer.OrdinalIgnoreCase)
            };
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Admin permission check denied for admin {AdminId} after an authorization lookup failure.", adminId);
            LogAuthorizationDenial(httpContext, hasAdminToken, hasAdminId, statusCode: null);
            return new AdminNavigationAccess();
        }
    }

    private void LogAuthorizationDenial(HttpContext? context, bool hasAdminToken, bool hasAdminId, int? statusCode)
    {
        var route = context?.Request.Path.Value ?? "unavailable";
        if (route.Length > 256)
            route = route[..256];

        _logger.LogInformation(
            "Admin navigation authorization lookup denied. TraceId={TraceId} Route={Route} HasAdminToken={HasAdminToken} HasAdminId={HasAdminId} HttpStatus={HttpStatus}",
            context?.TraceIdentifier ?? "unavailable",
            route,
            hasAdminToken,
            hasAdminId,
            statusCode);
    }

    private sealed record ApiResponse<T>(bool Success, string Message, T Data);
    private sealed record AdminAuthorizationAccessResponse(int RoleId, bool IsMaster, IReadOnlyCollection<string> Permissions);
}
