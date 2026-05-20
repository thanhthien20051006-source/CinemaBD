using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using CinemaBD.Web.Models;
using CinemaBD.Web.Services;
using Microsoft.AspNetCore.Http;

namespace CinemaBD.Web.Core;

public class AdminLegacyReadService : IAdminLegacyReadService
{
    private readonly HttpClient _httpClient;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public AdminLegacyReadService(HttpClient httpClient, IHttpContextAccessor httpContextAccessor)
    {
        _httpClient = httpClient;
        _httpContextAccessor = httpContextAccessor;
        if (_httpClient.BaseAddress == null) _httpClient.BaseAddress = new Uri("http://localhost:5188/");
    }

    private async Task<List<T>> GetAdminListAsync<T>(string url, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        var token = _httpContextAccessor.HttpContext?.Session.GetString("AdminToken");
        if (!string.IsNullOrWhiteSpace(token)) request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        using var response = await _httpClient.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode) return new List<T>();
        var payload = await response.Content.ReadFromJsonAsync<ApiResponse<JsonElement>>(cancellationToken: cancellationToken);
        if (payload?.Data.ValueKind == JsonValueKind.Array)
            return JsonSerializer.Deserialize<List<T>>(payload.Data.GetRawText(), new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new List<T>();
        return new List<T>();
    }

    public async Task<AdminListPageViewModel> GetServicesAsync(CancellationToken cancellationToken = default)
    {
        var data = await GetAdminListAsync<ComboRow>("api/admin/combos", cancellationToken);
        return new("Dịch vụ / Combo", new[] { "Mã", "Tên", "Giá", "Mô tả" }, data.Select(x => (IReadOnlyList<string>)new[] { x.Id, x.Name, x.Price.ToString("N0"), x.Description ?? "" }).ToList());
    }
    public async Task<AdminListPageViewModel> GetSeatsAsync(CancellationToken cancellationToken = default)
    {
        var data = await GetAdminListAsync<SeatRow>("api/admin/seats", cancellationToken);
        return new("Ghế", new[] { "Mã ghế", "Phòng", "Loại", "Trạng thái" }, data.Select(x => (IReadOnlyList<string>)new[] { x.Id, x.RoomId, x.SeatType ?? "", x.IsBooked ? "Không trống" : "Trống" }).ToList());
    }
    public async Task<AdminListPageViewModel> GetRoomsAsync(CancellationToken cancellationToken = default)
    {
        var data = await GetAdminListAsync<RoomRow>("api/admin/rooms", cancellationToken);
        return new("Phòng chiếu", new[] { "Mã phòng", "Tên phòng", "Số lượng", "Trạng thái" }, data.Select(x => (IReadOnlyList<string>)new[] { x.Id, x.Name, x.SeatCount.ToString(), x.Status ?? "" }).ToList());
    }
    public async Task<AdminListPageViewModel> GetGenresAsync(CancellationToken cancellationToken = default)
    {
        var data = await GetAdminListAsync<GenreRow>("api/admin/genres", cancellationToken);
        return new("Thể loại", new[] { "Mã", "Tên thể loại", "Mô tả" }, data.Select(x => (IReadOnlyList<string>)new[] { x.Id.ToString(), x.Name, x.Description ?? "" }).ToList());
    }
    public async Task<AdminListPageViewModel> GetArticlesAsync(CancellationToken cancellationToken = default)
    {
        var data = await GetAdminListAsync<ArticleRow>("api/admin/articles", cancellationToken);
        return new("Góc điện ảnh", new[] { "Mã", "Tiêu đề", "Mô tả", "Ngày đăng" }, data.Select(x => (IReadOnlyList<string>)new[] { x.Id.ToString(), x.Title, x.Summary ?? "", x.PublishedAt.ToString("dd/MM/yyyy") }).ToList());
    }
    public async Task<AdminListPageViewModel> GetEventsAsync(CancellationToken cancellationToken = default)
    {
        var data = await GetAdminListAsync<EventRow>("api/admin/events", cancellationToken);
        return new("Sự kiện", new[] { "Mã", "Tiêu đề", "Mô tả", "Bắt đầu", "Kết thúc" }, data.Select(x => (IReadOnlyList<string>)new[] { x.Id.ToString(), x.Title, x.Description ?? "", x.StartDate?.ToString("dd/MM/yyyy") ?? "", x.EndDate?.ToString("dd/MM/yyyy") ?? "" }).ToList());
    }
    public async Task<AdminListPageViewModel> GetEmployeesAsync(CancellationToken cancellationToken = default)
    {
        var data = await GetAdminListAsync<EmployeeRow>("api/admin/employees", cancellationToken);
        return new("Nhân viên", new[] { "Mã", "Họ tên", "Email", "SĐT", "Trạng thái" }, data.Select(x => (IReadOnlyList<string>)new[] { x.Id.ToString(), x.FullName, x.Email ?? "", x.PhoneNumber ?? "", x.IsActive ? "Hoạt động" : "Khóa" }).ToList());
    }
    public async Task<AdminListPageViewModel> GetRolesAsync(CancellationToken cancellationToken = default)
    {
        var data = await GetAdminListAsync<RoleRow>("api/admin/roles", cancellationToken);
        return new("Chức vụ", new[] { "Mã", "Tên chức vụ", "Master", "Trạng thái" }, data.Select(x => (IReadOnlyList<string>)new[] { x.Id.ToString(), x.Name, x.IsMaster ? "Có" : "Không", x.IsActive ? "Hoạt động" : "Khóa" }).ToList());
    }
    public async Task<AdminListPageViewModel> GetRolePermissionsAsync(CancellationToken cancellationToken = default)
    {
        var data = await GetAdminListAsync<PermissionRow>("api/admin/roles/permissions", cancellationToken);
        return new("Phân quyền", new[] { "Mã", "Chức năng" }, data.Select(x => (IReadOnlyList<string>)new[] { x.Id.ToString(), x.Name }).ToList());
    }
    public async Task<AdminListPageViewModel> GetInvoicesAsync(CancellationToken cancellationToken = default)
    {
        var data = await GetAdminListAsync<InvoiceRow>("api/admin/invoices", cancellationToken);
        return new("Hóa đơn", new[] { "Mã hóa đơn", "Khách hàng", "Giao dịch", "Tổng tiền", "Ngày", "Trạng thái" }, data.Select(x => (IReadOnlyList<string>)new[] { x.InvoiceId, x.CustomerName ?? "", x.TransactionRef ?? "", x.TotalAmount.ToString("N0"), x.PaymentDate.ToString("dd/MM/yyyy HH:mm"), x.Status ?? "" }).ToList());
    }
    public async Task<AdminThongKeViewModel> GetStatisticsAsync(CancellationToken cancellationToken = default)
    {
        var data = await GetAdminListAsync<RevenueRow>("api/admin/statistics/revenue", cancellationToken);
        return new AdminThongKeViewModel { Cards = new[] { new AdminMetricCard("Doanh thu", "Xem Dashboard"), new AdminMetricCard("Đơn vé", "Xem hóa đơn"), new AdminMetricCard("Khách hàng", "Xem người dùng"), new AdminMetricCard("Phim", "Xem phim") } };
    }

    private record ComboRow(string Id, string Name, decimal Price, string? Description);
    private record SeatRow(string Id, string RoomId, string? SeatType, bool IsBooked);
    private record RoomRow(string Id, string Name, int SeatCount, string? Status);
    private record GenreRow(int Id, string Name, string? Description);
    private record ArticleRow(int Id, string Title, string? Summary, DateTime PublishedAt);
    private record EventRow(int Id, string Title, string? Description, DateTime? StartDate, DateTime? EndDate);
    private record EmployeeRow(int Id, string FullName, string? Email, string? PhoneNumber, bool IsActive);
    private record RoleRow(int Id, string Name, bool IsMaster, bool IsActive);
    private record PermissionRow(int Id, string Name);
    private record InvoiceRow(string InvoiceId, string? CustomerName, string? TransactionRef, decimal TotalAmount, DateTime PaymentDate, string? Status);
    private record RevenueRow(decimal TotalRevenue, int TotalOrders);
}


