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
        if (string.IsNullOrWhiteSpace(token) || !int.TryParse(httpContext?.Session.GetString("AdminId"), out var adminId) || adminId <= 0)
        {
            _logger.LogWarning("Admin permission check denied because authenticated role context is missing.");
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
                return new AdminNavigationAccess();
            }

            var payload = await response.Content.ReadFromJsonAsync<ApiResponse<JsonElement>>(cancellationToken: cancellationToken);
            if (payload is not { Success: true } || payload.Data.ValueKind is JsonValueKind.Undefined or JsonValueKind.Null)
            {
                _logger.LogWarning("Admin permission check denied for admin {AdminId}; authorization response was invalid.", adminId);
                return new AdminNavigationAccess();
            }

            var access = JsonSerializer.Deserialize<AdminAuthorizationAccessResponse>(payload.Data.GetRawText(), new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            if (access is null || access.RoleId <= 0 || access.Permissions is null)
            {
                _logger.LogWarning("Admin permission check denied for admin {AdminId}; authorization response was invalid.", adminId);
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
            return new AdminNavigationAccess();
        }
    }

    private sealed record ApiResponse<T>(bool Success, string Message, T Data);
    private sealed record AdminAuthorizationAccessResponse(int RoleId, bool IsMaster, IReadOnlyCollection<string> Permissions);
}
