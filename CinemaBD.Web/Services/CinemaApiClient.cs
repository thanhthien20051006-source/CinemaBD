using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using CinemaBD.Web.Models;
using Microsoft.AspNetCore.Http;

namespace CinemaBD.Web.Services;

public class CinemaApiClient
{
    private readonly HttpClient _httpClient;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CinemaApiClient(HttpClient httpClient, IHttpContextAccessor httpContextAccessor)
    {
        _httpClient = httpClient;
        _httpContextAccessor = httpContextAccessor;

        var baseAddress = _httpClient.BaseAddress?.ToString() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(baseAddress) ||
            baseAddress.Contains("localhost:5001", StringComparison.OrdinalIgnoreCase) ||
            baseAddress.Contains("127.0.0.1:5001", StringComparison.OrdinalIgnoreCase))
        {
            _httpClient.BaseAddress = new Uri("http://localhost:5188/");
        }
    }

    public async Task<IReadOnlyList<MovieViewModel>> GetMoviesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.GetFromJsonAsync<ApiResponse<List<MovieViewModel>>>("api/movies", cancellationToken);
            return NormalizeMoviePosters(response?.Data ?? new List<MovieViewModel>());
        }
        catch (HttpRequestException)
        {
            return new List<MovieViewModel>();
        }
    }

    public async Task<MovieViewModel?> GetMovieByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.GetFromJsonAsync<ApiResponse<MovieViewModel>>($"api/movies/{id}", cancellationToken);
        return NormalizeMoviePoster(response?.Data);
    }

    public async Task<IReadOnlyList<ShowtimeViewModel>> GetShowtimesAsync(string movieId, DateTime date, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.GetFromJsonAsync<ApiResponse<List<ShowtimeViewModel>>>($"api/movies/{movieId}/showtimes?date={date:yyyy-MM-dd}", cancellationToken);
        return response?.Data ?? new List<ShowtimeViewModel>();
    }

    public async Task<IReadOnlyList<SeatViewModel>> GetSeatsAsync(string showtimeId, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.GetFromJsonAsync<ApiResponse<List<SeatViewModel>>>($"api/bookings/showtimes/{showtimeId}/seats", cancellationToken);
            return response?.Data ?? new List<SeatViewModel>();
        }
        catch (HttpRequestException)
        {
            return new List<SeatViewModel>();
        }
    }

    public async Task<IReadOnlyList<ComboViewModel>> GetCombosAsync(CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.GetFromJsonAsync<ApiResponse<List<ComboViewModel>>>("api/combos", cancellationToken);
        return response?.Data ?? new List<ComboViewModel>();
    }

    public async Task<AuthResult?> LoginAsync(string username, string password, CancellationToken cancellationToken = default)
    {
        try
        {
            using var response = await _httpClient.PostAsJsonAsync("api/auth/login", new { username, password }, cancellationToken);
            if (!response.IsSuccessStatusCode)
                return null;

            var payload = await response.Content.ReadFromJsonAsync<ApiResponse<AuthResult>>(cancellationToken: cancellationToken);
            return payload?.Data;
        }
        catch (HttpRequestException)
        {
            return null;
        }
    }

    public async Task<AdminAuthResult?> AdminLoginAsync(string username, string password, CancellationToken cancellationToken = default)
    {
        try
        {
            using var response = await _httpClient.PostAsJsonAsync("api/admin/auth/login", new { username, password }, cancellationToken);
            if (!response.IsSuccessStatusCode)
                return null;

            var payload = await response.Content.ReadFromJsonAsync<ApiResponse<AdminAuthResult>>(cancellationToken: cancellationToken);
            return payload?.Data;
        }
        catch (HttpRequestException)
        {
            return null;
        }
    }

    public async Task<AuthResult?> RegisterAsync(string fullName, string username, string password, string? email, string? phoneNumber, CancellationToken cancellationToken = default)
    {
        using var response = await _httpClient.PostAsJsonAsync("api/auth/register", new
        {
            fullName,
            username,
            password,
            email,
            phoneNumber
        }, cancellationToken);

        if (!response.IsSuccessStatusCode)
            return null;

        var payload = await response.Content.ReadFromJsonAsync<ApiResponse<AuthResult>>(cancellationToken: cancellationToken);
        return payload?.Data;
    }

    public async Task<AdminLoyaltyPointViewModel?> GetLoyaltyAsync(string token, CancellationToken cancellationToken = default)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "api/account/loyalty");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        using var response = await _httpClient.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode) return null;
        var payload = await response.Content.ReadFromJsonAsync<ApiResponse<AdminLoyaltyPointViewModel>>(cancellationToken: cancellationToken);
        return payload?.Data;
    }

    public async Task<LoyaltyRedeemApiResult?> PreviewRedeemPointsAsync(string token, int points, decimal subtotal, CancellationToken cancellationToken = default)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "api/account/loyalty/preview-redeem");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        request.Content = JsonContent.Create(new { points, subtotal });
        using var response = await _httpClient.SendAsync(request, cancellationToken);
        var payload = await response.Content.ReadFromJsonAsync<ApiResponse<LoyaltyRedeemApiResult>>(cancellationToken: cancellationToken);
        return payload?.Data;
    }

    public async Task<RefundApiResult?> CreateRefundRequestAsync(string token, string transactionRef, string? ticketId, string reason, CancellationToken cancellationToken = default)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "api/bookings/refund-request");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        request.Content = JsonContent.Create(new { transactionRef, ticketId, reason });
        using var response = await _httpClient.SendAsync(request, cancellationToken);
        var payload = await response.Content.ReadFromJsonAsync<ApiResponse<RefundApiResult>>(cancellationToken: cancellationToken);
        return payload?.Data ?? new RefundApiResult { Success = false, Message = payload?.Message ?? "Không gửi được yêu cầu hủy vé." };
    }

    public async Task<SeatHoldApiResult?> HoldSeatsAsync(string token, string showtimeId, IReadOnlyList<string> seats, CancellationToken cancellationToken = default)
    {
        return await SendSeatHoldAsync(token, "api/bookings/hold-seats", showtimeId, seats, cancellationToken);
    }

    public async Task<SeatHoldApiResult?> ReleaseSeatsAsync(string token, string showtimeId, IReadOnlyList<string> seats, CancellationToken cancellationToken = default)
    {
        return await SendSeatHoldAsync(token, "api/bookings/release-seats", showtimeId, seats, cancellationToken);
    }

    private async Task<SeatHoldApiResult?> SendSeatHoldAsync(string token, string url, string showtimeId, IReadOnlyList<string> seats, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, url);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        request.Content = new StringContent(JsonSerializer.Serialize(new { showtimeId, seats }), Encoding.UTF8, "application/json");
        using var response = await _httpClient.SendAsync(request, cancellationToken);
        var payload = await response.Content.ReadFromJsonAsync<ApiResponse<SeatHoldApiResult>>(cancellationToken: cancellationToken);
        return payload?.Data;
    }

    public async Task<(string TransactionRef, string PaymentUrl, decimal TotalAmount)?> CheckoutAsync(string token, string showtimeId, IReadOnlyList<string> seats, string? combos, decimal totalAmount, CancellationToken cancellationToken = default)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "api/bookings/checkout");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        request.Headers.TryAddWithoutValidation("X-User-Id", token);
        request.Content = new StringContent(JsonSerializer.Serialize(new
        {
            showtimeId,
            seats,
            combos,
            totalAmount
        }), Encoding.UTF8, "application/json");

        using var response = await _httpClient.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            Console.WriteLine($"Checkout API failed: {(int)response.StatusCode} {body}");
            return null;
        }

        var payload = await response.Content.ReadFromJsonAsync<ApiResponse<CheckoutApiResult>>(cancellationToken: cancellationToken);
        var data = payload?.Data;
        return data == null ? null : (data.TransactionRef, data.PaymentUrl, data.TotalAmount);
    }

    public async Task<UserProfileViewModel?> GetProfileAsync(string token, CancellationToken cancellationToken = default)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "api/account/profile");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        request.Headers.TryAddWithoutValidation("X-User-Id", token);
        using var response = await _httpClient.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
            return null;

        var payload = await response.Content.ReadFromJsonAsync<ApiResponse<UserProfileViewModel>>(cancellationToken: cancellationToken);
        return payload?.Data;
    }

    public async Task<UserProfileViewModel?> UpdateProfileAsync(string token, UserProfileViewModel model, CancellationToken cancellationToken = default)
    {
        using var request = new HttpRequestMessage(HttpMethod.Put, "api/account/profile");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        request.Headers.TryAddWithoutValidation("X-User-Id", token);
        request.Content = JsonContent.Create(new
        {
            fullName = model.FullName,
            email = model.Email,
            phoneNumber = model.PhoneNumber,
            birthDate = model.BirthDate
        });

        using var response = await _httpClient.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
            return null;

        var payload = await response.Content.ReadFromJsonAsync<ApiResponse<UserProfileViewModel>>(cancellationToken: cancellationToken);
        return payload?.Data;
    }

    public async Task<IReadOnlyList<InvoiceHistoryItem>> GetHistoryAsync(string token, CancellationToken cancellationToken = default)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "api/account/history");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        request.Headers.TryAddWithoutValidation("X-User-Id", token);
        using var response = await _httpClient.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
            return Array.Empty<InvoiceHistoryItem>();

        var payload = await response.Content.ReadFromJsonAsync<ApiResponse<List<InvoiceHistoryItem>>>(cancellationToken: cancellationToken);
        return payload?.Data ?? new List<InvoiceHistoryItem>();
    }

    public async Task<InvoiceViewModel?> GetInvoiceAsync(string txnRef, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.GetFromJsonAsync<ApiResponse<InvoiceViewModel>>($"api/bookings/invoice/{Uri.EscapeDataString(txnRef)}", cancellationToken);
            if (response?.Data != null)
                response.Data.MoviePosterUrl = NormalizePosterUrlValue(response.Data.MoviePosterUrl);

            return response?.Data;
        }
        catch (HttpRequestException)
        {
            return null;
        }
    }

    public async Task<InvoiceViewModel?> GetInvoiceAsync(string token, string txnRef, CancellationToken cancellationToken = default)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, $"api/bookings/invoice/{Uri.EscapeDataString(txnRef)}");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        using var response = await _httpClient.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode) return null;

        var payload = await response.Content.ReadFromJsonAsync<ApiResponse<InvoiceViewModel>>(cancellationToken: cancellationToken);
        if (payload?.Data != null)
            payload.Data.MoviePosterUrl = NormalizePosterUrlValue(payload.Data.MoviePosterUrl);

        return payload?.Data;
    }

    public async Task<PaymentCallbackResult?> ConfirmPaymentAsync(IDictionary<string, string> query, CancellationToken cancellationToken = default)
    {
        var url = "api/payments/vnpay-return";
        if (query.Count > 0)
        {
            var qs = string.Join("&", query.Select(x => $"{Uri.EscapeDataString(x.Key)}={Uri.EscapeDataString(x.Value)}"));
            url += "?" + qs;
        }

        using var response = await _httpClient.GetAsync(url, cancellationToken);
        if (!response.IsSuccessStatusCode) return null;

        var payload = await response.Content.ReadFromJsonAsync<ApiResponse<PaymentCallbackResult>>(cancellationToken: cancellationToken);
        return payload?.Data;
    }

    public async Task<PaymentCallbackResult?> ConfirmDemoPaymentAsync(string txnRef, string gateway = "MOMO", CancellationToken cancellationToken = default)
    {
        var url = $"api/payments/demo-confirm?txnRef={Uri.EscapeDataString(txnRef)}&gateway={Uri.EscapeDataString(gateway)}";
        using var response = await _httpClient.GetAsync(url, cancellationToken);
        if (!response.IsSuccessStatusCode) return null;

        var payload = await response.Content.ReadFromJsonAsync<ApiResponse<PaymentCallbackResult>>(cancellationToken: cancellationToken);
        return payload?.Data;
    }

    public async Task<IReadOnlyList<ReviewItem>> GetReviewsAsync(string movieId, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.GetFromJsonAsync<ApiResponse<List<ReviewItem>>>($"api/reviews/movie/{movieId}", cancellationToken);
        return response?.Data ?? new List<ReviewItem>();
    }

    public async Task<ReviewItem?> GetReviewEligibilityAsync(string token, string movieId, CancellationToken cancellationToken = default)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, $"api/reviews/movie/{Uri.EscapeDataString(movieId)}/eligibility");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        using var response = await _httpClient.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode) return null;
        var payload = await response.Content.ReadFromJsonAsync<ApiResponse<ReviewItem>>(cancellationToken: cancellationToken);
        return payload?.Data;
    }

    public async Task<(bool Success, string Message)> CreateReviewAsync(string token, string movieId, string content, int rating, CancellationToken cancellationToken = default)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "api/reviews");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        request.Content = JsonContent.Create(new { movieId, content, rating });

        using var response = await _httpClient.SendAsync(request, cancellationToken);
        var payload = await response.Content.ReadFromJsonAsync<ApiResponse<ReviewItem>>(cancellationToken: cancellationToken);
        return (payload?.Success == true, payload?.Message ?? (response.IsSuccessStatusCode ? "Gửi đánh giá thành công." : "Gửi đánh giá thất bại."));
    }

    public async Task<IReadOnlyList<ReviewItem>> GetAdminReviewsAsync(string token, CancellationToken cancellationToken = default)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "api/reviews/admin");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        using var response = await _httpClient.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode) return Array.Empty<ReviewItem>();

        var payload = await response.Content.ReadFromJsonAsync<ApiResponse<List<ReviewItem>>>(cancellationToken: cancellationToken);
        return payload?.Data ?? new List<ReviewItem>();
    }

    public async Task<bool> ToggleAdminReviewHiddenAsync(string token, int id, CancellationToken cancellationToken = default)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, $"api/reviews/admin/{id}/toggle-hidden");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        using var response = await _httpClient.SendAsync(request, cancellationToken);
        return response.IsSuccessStatusCode;
    }

    public async Task<bool> DeleteAdminReviewAsync(string token, int id, CancellationToken cancellationToken = default)
    {
        using var request = new HttpRequestMessage(HttpMethod.Delete, $"api/reviews/admin/{id}");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        using var response = await _httpClient.SendAsync(request, cancellationToken);
        return response.IsSuccessStatusCode;
    }

    public async Task<HomeIndexViewModel> GetHomeDataAsync(int showingPage = 1, int upcomingPage = 1, int pageSize = 4, CancellationToken cancellationToken = default)
    {
        var movies = await GetMoviesAsync(cancellationToken);
        var showing = movies.Where(m => string.Equals(m.Status, "Đang chiếu", StringComparison.OrdinalIgnoreCase) || string.Equals(m.Status, "Dang chieu", StringComparison.OrdinalIgnoreCase)).ToList();
        var upcoming = movies.Where(m => string.Equals(m.Status, "Sắp chiếu", StringComparison.OrdinalIgnoreCase) || string.Equals(m.Status, "Sap chieu", StringComparison.OrdinalIgnoreCase)).ToList();

        if (showing.Count == 0)
            showing = movies.ToList();
        if (upcoming.Count == 0)
            upcoming = movies.Except(showing).ToList();

        showingPage = Math.Max(1, showingPage);
        upcomingPage = Math.Max(1, upcomingPage);
        pageSize = Math.Max(1, pageSize);

        var showingTotalPages = Math.Max(1, (int)Math.Ceiling(showing.Count / (double)pageSize));
        var upcomingTotalPages = Math.Max(1, (int)Math.Ceiling(upcoming.Count / (double)pageSize));

        showingPage = Math.Min(showingPage, showingTotalPages);
        upcomingPage = Math.Min(upcomingPage, upcomingTotalPages);

        return new HomeIndexViewModel
        {
            ShowingMovies = showing.Skip((showingPage - 1) * pageSize).Take(pageSize).ToList(),
            UpcomingMovies = upcoming.Skip((upcomingPage - 1) * pageSize).Take(pageSize).ToList(),
            ShowingPage = showingPage,
            UpcomingPage = upcomingPage,
            PageSize = pageSize,
            ShowingTotalPages = showingTotalPages,
            UpcomingTotalPages = upcomingTotalPages
        };
    }

    public sealed class AuthResult
    {
        public string UserId { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Token { get; set; } = string.Empty;
    }

    public sealed class AdminAuthResult
    {
        public int AdminId { get; set; }
        public string Username { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string? Role { get; set; }
        public string Token { get; set; } = string.Empty;
    }

    public sealed class InvoiceHistoryItem
    {
        public string InvoiceId { get; set; } = string.Empty;
        public string? MovieTitle { get; set; }
        public DateTime? ShowDate { get; set; }
        public TimeSpan? StartTime { get; set; }
        public decimal TotalAmount { get; set; }
        public string? Status { get; set; }
        public DateTime PaymentDate { get; set; }
        public int TicketCount { get; set; }
        public int CheckedInCount { get; set; }
        public List<string> SeatIds { get; set; } = new();
    }

    public sealed class ReviewItem
    {
        public int Id { get; set; }
        public string MovieId { get; set; } = string.Empty;
        public string CustomerId { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public int Rating { get; set; } = 5;
        public bool IsHidden { get; set; }
        public bool CanReview { get; set; }
        public string? ReviewRuleMessage { get; set; }
        public DateTime CreatedAt { get; set; }
        public string? MovieTitle { get; set; }
        public string? CustomerName { get; set; }
    }

    private List<MovieViewModel> NormalizeMoviePosters(List<MovieViewModel> movies)
    {
        foreach (var movie in movies)
            NormalizeMoviePoster(movie);

        return movies;
    }

    private string NormalizePosterUrlValue(string? posterUrl)
    {
        if (string.IsNullOrWhiteSpace(posterUrl))
            return string.Empty;

        if (Uri.TryCreate(posterUrl, UriKind.Absolute, out _))
            return posterUrl;

        var fileName = Path.GetFileName(posterUrl.Replace('\\', '/'));
        if (string.IsNullOrWhiteSpace(fileName))
            return string.Empty;

        var request = _httpContextAccessor.HttpContext?.Request;
        var origin = request is null ? string.Empty : $"{request.Scheme}://{request.Host}";
        return $"{origin}/legacy/Content/img/Posters/{fileName}";
    }

    private MovieViewModel? NormalizeMoviePoster(MovieViewModel? movie)
    {
        if (movie is null)
            return movie;

        movie.PosterUrl = NormalizePosterUrlValue(movie.PosterUrl);
        return movie;
    }

    public sealed class RefundApiResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
    }

    public sealed class LoyaltyRedeemApiResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public int UsedPoints { get; set; }
        public decimal DiscountAmount { get; set; }
        public int RemainingPoints { get; set; }
    }

    public sealed class SeatHoldApiResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public string ShowtimeId { get; set; } = string.Empty;
        public List<string> HeldSeats { get; set; } = new();
        public List<string> UnavailableSeats { get; set; } = new();
        public DateTime? ExpiresAt { get; set; }
    }

    private sealed class CheckoutApiResult
    {
        public string TransactionRef { get; set; } = string.Empty;
        public string PaymentUrl { get; set; } = string.Empty;
        public decimal TotalAmount { get; set; }
    }

    public sealed class PaymentCallbackResult
    {
        public bool Success { get; set; }
        public bool SignatureValid { get; set; }
        public string? TransactionRef { get; set; }
        public string? ResponseCode { get; set; }
        public string Message { get; set; } = string.Empty;
    }
}




