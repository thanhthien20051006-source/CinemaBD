using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using CinemaBD.Web.Models;

namespace CinemaBD.Web.Core;

public interface IAdminNavigationService
{
    Task<AdminNavigationAccess> GetAccessAsync(CancellationToken cancellationToken = default);
}

public class AdminNavigationAccess
{
    public bool AllowAll { get; set; }
    public HashSet<string> Permissions { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    public bool Can(params string[] keywords)
    {
        if (AllowAll) return true;
        if (Permissions.Count == 0) return true;
        return keywords.Any(keyword => Permissions.Any(permission => Normalize(permission).Contains(Normalize(keyword))));
    }

    private static string Normalize(string? value) => (value ?? string.Empty)
        .Trim()
        .ToLowerInvariant()
        .Replace("đ", "d")
        .Replace(" ", "")
        .Replace("_", "")
        .Replace("-", "");
}

public class AdminNavigationService : IAdminNavigationService
{
    private readonly HttpClient _httpClient;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public AdminNavigationService(HttpClient httpClient, IHttpContextAccessor httpContextAccessor)
    {
        _httpClient = httpClient;
        _httpContextAccessor = httpContextAccessor;
        if (_httpClient.BaseAddress == null) _httpClient.BaseAddress = new Uri("http://localhost:5188/");
    }

    public async Task<AdminNavigationAccess> GetAccessAsync(CancellationToken cancellationToken = default)
    {
        var httpContext = _httpContextAccessor.HttpContext;
        var roleName = httpContext?.Session.GetString("AdminRole") ?? string.Empty;
        var token = httpContext?.Session.GetString("AdminToken") ?? string.Empty;

        if (string.IsNullOrWhiteSpace(roleName) || IsMasterRole(roleName))
            return new AdminNavigationAccess { AllowAll = true };

        try
        {
            var roles = await SendAsync<List<RoleFormViewModel>>("api/admin/roles", token, cancellationToken) ?? new();
            var role = roles.FirstOrDefault(x => string.Equals(x.Name, roleName, StringComparison.OrdinalIgnoreCase));
            if (role == null || role.IsMaster)
                return new AdminNavigationAccess { AllowAll = true };

            var permissions = await SendAsync<List<PermissionViewModel>>($"api/admin/roles/{role.Id}/permissions", token, cancellationToken) ?? new();
            return new AdminNavigationAccess
            {
                AllowAll = permissions.Count == 0,
                Permissions = permissions.Select(x => x.Name).ToHashSet(StringComparer.OrdinalIgnoreCase)
            };
        }
        catch
        {
            return new AdminNavigationAccess { AllowAll = true };
        }
    }

    private async Task<T?> SendAsync<T>(string url, string token, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        if (!string.IsNullOrWhiteSpace(token))
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        using var response = await _httpClient.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode) return default;

        var payload = await response.Content.ReadFromJsonAsync<ApiResponse<JsonElement>>(cancellationToken: cancellationToken);
        if (payload?.Data.ValueKind is JsonValueKind.Undefined or JsonValueKind.Null) return default;
        return JsonSerializer.Deserialize<T>(payload!.Data.GetRawText(), new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
    }

    private static bool IsMasterRole(string roleName)
    {
        var role = roleName.Trim().ToLowerInvariant();
        return role is "admin" or "administrator" or "master" or "quan tri" or "quản trị";
    }
}

