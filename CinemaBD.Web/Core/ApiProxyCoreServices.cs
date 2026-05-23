using System.Net.Http.Headers;
using System.Net.Http.Json;
using CinemaBD.Web.Models;

namespace CinemaBD.Web.Core;

public class ApiAuthCoreService : IAuthCoreService
{
    private readonly HttpClient _http;
    public ApiAuthCoreService(HttpClient http) => _http = http;

    public async Task<AuthResponse?> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default)
        => await PostDataAsync<AuthResponse>("api/auth/login", request, cancellationToken);

    public async Task<AuthResponse?> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default)
        => await PostDataAsync<AuthResponse>("api/auth/register", request, cancellationToken);

    public async Task<UserProfileViewModel?> GetProfileAsync(string userId, CancellationToken cancellationToken = default)
        => await GetWithUserAsync<UserProfileViewModel>("api/account/profile", userId, cancellationToken);

    public async Task<IReadOnlyList<InvoiceHistoryItem>> GetHistoryAsync(string userId, CancellationToken cancellationToken = default)
        => await GetWithUserAsync<List<InvoiceHistoryItem>>("api/account/history", userId, cancellationToken) ?? new();

    private async Task<T?> GetWithUserAsync<T>(string url, string userId, CancellationToken ct)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", userId);
        request.Headers.TryAddWithoutValidation("X-User-Id", userId);
        using var response = await _http.SendAsync(request, ct);
        if (!response.IsSuccessStatusCode) return default;
        var payload = await response.Content.ReadFromJsonAsync<ApiResponse<T>>(cancellationToken: ct);
        return payload == null ? default : payload.Data;
    }

    private async Task<T?> PostDataAsync<T>(string url, object body, CancellationToken ct)
    {
        using var response = await _http.PostAsJsonAsync(url, body, ct);
        if (!response.IsSuccessStatusCode) return default;
        var payload = await response.Content.ReadFromJsonAsync<ApiResponse<T>>(cancellationToken: ct);
        return payload == null ? default : payload.Data;
    }
}

public class ApiBookingCoreService : IBookingCoreService
{
    private readonly HttpClient _http;
    public ApiBookingCoreService(HttpClient http) => _http = http;

    public async Task<IReadOnlyList<MovieViewModel>> GetMoviesAsync(CancellationToken cancellationToken = default)
        => await GetDataAsync<List<MovieViewModel>>("api/movies", cancellationToken) ?? new();

    public async Task<MovieViewModel?> GetMovieByIdAsync(string id, CancellationToken cancellationToken = default)
        => await GetDataAsync<MovieViewModel>($"api/movies/{Uri.EscapeDataString(id)}", cancellationToken);

    public async Task<IReadOnlyList<ShowtimeViewModel>> GetShowtimesAsync(string movieId, DateTime date, CancellationToken cancellationToken = default)
        => await GetDataAsync<List<ShowtimeViewModel>>($"api/movies/{Uri.EscapeDataString(movieId)}/showtimes?date={date:yyyy-MM-dd}", cancellationToken) ?? new();

    public async Task<IReadOnlyList<SeatViewModel>> GetSeatsAsync(string showtimeId, CancellationToken cancellationToken = default)
        => await GetDataAsync<List<SeatViewModel>>($"api/bookings/showtimes/{Uri.EscapeDataString(showtimeId)}/seats", cancellationToken) ?? new();

    public async Task<IReadOnlyList<ComboViewModel>> GetCombosAsync(CancellationToken cancellationToken = default)
        => await GetDataAsync<List<ComboViewModel>>("api/combos", cancellationToken) ?? new();

    public async Task<CheckoutResponse?> CheckoutAsync(string userId, CheckoutRequest request, CancellationToken cancellationToken = default)
    {
        using var message = new HttpRequestMessage(HttpMethod.Post, "api/bookings/checkout");
        message.Headers.Authorization = new AuthenticationHeaderValue("Bearer", userId);
        message.Headers.TryAddWithoutValidation("X-User-Id", userId);
        message.Content = JsonContent.Create(request);
        using var response = await _http.SendAsync(message, cancellationToken);
        if (!response.IsSuccessStatusCode) return null;
        var payload = await response.Content.ReadFromJsonAsync<ApiResponse<CheckoutResponse>>(cancellationToken: cancellationToken);
        return payload == null ? default : payload.Data;
    }

    public async Task<bool> ConfirmPaymentAsync(string txnRef, string? responseCode, CancellationToken cancellationToken = default)
    {
        var url = $"api/payments/vnpay-return?vnp_TxnRef={Uri.EscapeDataString(txnRef)}&vnp_ResponseCode={Uri.EscapeDataString(responseCode ?? string.Empty)}";
        using var response = await _http.GetAsync(url, cancellationToken);
        return response.IsSuccessStatusCode;
    }

    public async Task<InvoiceViewModel?> GetInvoiceAsync(string txnRef, CancellationToken cancellationToken = default)
        => await GetDataAsync<InvoiceViewModel>($"api/bookings/invoice/{Uri.EscapeDataString(txnRef)}", cancellationToken);

    private async Task<T?> GetDataAsync<T>(string url, CancellationToken ct)
    {
        var payload = await _http.GetFromJsonAsync<ApiResponse<T>>(url, ct);
        return payload == null ? default : payload.Data;
    }
}

