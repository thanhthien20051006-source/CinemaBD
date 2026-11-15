using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Http;

namespace CinemaBD.Web.Services;

/// <summary>For MVC API calls, refreshes once on 401 and persists the rotated session credentials.</summary>
public sealed class ApiSessionTokenRefreshHandler(IHttpContextAccessor contextAccessor, ILogger<ApiSessionTokenRefreshHandler> logger) : DelegatingHandler
{
    private const string UserTokenKey = "UserToken";
    private const string AdminTokenKey = "AdminToken";
    private const string UserRefreshKey = "UserRefreshToken";
    private const string AdminRefreshKey = "AdminRefreshToken";

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var context = contextAccessor.HttpContext;
        var path = request.RequestUri?.AbsolutePath ?? string.Empty;
        if (context is null || IsAuthPath(path))
            return await base.SendAsync(request, cancellationToken);

        var isAdmin = string.Equals(context.Session.GetString("LoginType"), "Admin", StringComparison.OrdinalIgnoreCase);
        var tokenKey = isAdmin ? AdminTokenKey : UserTokenKey;
        var refreshKey = isAdmin ? AdminRefreshKey : UserRefreshKey;
        var currentToken = context.Session.GetString(tokenKey);
        if (!string.IsNullOrWhiteSpace(currentToken) && request.Headers.Authorization is null)
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", currentToken);

        var first = await base.SendAsync(request, cancellationToken);
        if (first.StatusCode != System.Net.HttpStatusCode.Unauthorized || string.IsNullOrWhiteSpace(currentToken))
            return first;

        var refreshToken = context.Session.GetString(refreshKey);
        if (string.IsNullOrWhiteSpace(refreshToken)) return first;

        RefreshResult? data;
        try
        {
            using var refreshClient = new HttpClient { BaseAddress = new Uri(request.RequestUri!.GetLeftPart(UriPartial.Authority) + "/") };
            var refreshPath = isAdmin ? "api/admin/auth/refresh" : "api/auth/refresh";
            using var refreshResponse = await refreshClient.PostAsJsonAsync(refreshPath, new { accessToken = currentToken, refreshToken }, cancellationToken);
            if (!refreshResponse.IsSuccessStatusCode) return first;
            var envelope = await refreshResponse.Content.ReadFromJsonAsync<ApiResponse<RefreshResult>>(cancellationToken: cancellationToken);
            data = envelope?.Data;
            if (data is null || string.IsNullOrWhiteSpace(data.Token) || string.IsNullOrWhiteSpace(data.RefreshToken)) return first;
        }
        catch (Exception ex) when (ex is HttpRequestException or System.Text.Json.JsonException)
        {
            logger.LogWarning("API refresh request failed; retaining current response status {StatusCode}.", (int)first.StatusCode);
            return first;
        }

        context.Session.SetString(tokenKey, data.Token);
        context.Session.SetString(refreshKey, data.RefreshToken);
        if (data.SessionId != Guid.Empty) context.Session.SetString(isAdmin ? "AdminSessionId" : "UserSessionId", data.SessionId.ToString("D"));

        var retry = await CloneRequestAsync(request, cancellationToken);
        retry.Headers.Authorization = new AuthenticationHeaderValue("Bearer", data.Token);
        first.Dispose();
        return await base.SendAsync(retry, cancellationToken);
    }

    private static bool IsAuthPath(string path)
        => path.Contains("/auth/login", StringComparison.OrdinalIgnoreCase)
            || path.Contains("/auth/register", StringComparison.OrdinalIgnoreCase)
            || path.Contains("/auth/google", StringComparison.OrdinalIgnoreCase)
            || path.Contains("/auth/refresh", StringComparison.OrdinalIgnoreCase);

    private static async Task<HttpRequestMessage> CloneRequestAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var clone = new HttpRequestMessage(request.Method, request.RequestUri) { Version = request.Version, VersionPolicy = request.VersionPolicy };
        foreach (var header in request.Headers)
            if (!string.Equals(header.Key, "Authorization", StringComparison.OrdinalIgnoreCase)) clone.Headers.TryAddWithoutValidation(header.Key, header.Value);
        if (request.Content is not null)
        {
            var content = new ByteArrayContent(await request.Content.ReadAsByteArrayAsync(cancellationToken));
            foreach (var header in request.Content.Headers) content.Headers.TryAddWithoutValidation(header.Key, header.Value);
            clone.Content = content;
        }
        foreach (var option in request.Options) clone.Options.Set(new HttpRequestOptionsKey<object?>(option.Key), option.Value);
        return clone;
    }

    private sealed record ApiResponse<T>([property: JsonPropertyName("success")] bool Success, [property: JsonPropertyName("message")] string Message, [property: JsonPropertyName("data")] T? Data);
    private sealed record RefreshResult([property: JsonPropertyName("token")] string Token, [property: JsonPropertyName("refreshToken")] string RefreshToken, [property: JsonPropertyName("sessionId")] Guid SessionId);
}
