using CinemaBD.Application.Interfaces;
using CinemaBD.Domain.Entities;
using CinemaBD.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CinemaBD.Infrastructure.Services;

public sealed class VoucherPreviewService : IVoucherPreviewService
{
    private readonly AppDbContext _db;
    private readonly IAdminVoucherService _voucherService;

    public VoucherPreviewService(AppDbContext db, IAdminVoucherService voucherService)
    {
        _db = db;
        _voucherService = voucherService;
    }

    public async Task<VoucherValidationResult> PreviewAsync(
        string customerId,
        string showtimeId,
        IReadOnlyCollection<string> seats,
        string? combos,
        string code,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(customerId))
            return Fail("Cần đăng nhập để dùng voucher.");
        if (string.IsNullOrWhiteSpace(showtimeId) || seats == null || seats.Count == 0)
            return Fail("Vui lòng chọn suất chiếu và ghế trước khi áp dụng voucher.");
        if (string.IsNullOrWhiteSpace(code))
            return Fail("Chưa nhập mã voucher.");

        var selectedSeats = seats
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => x.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        if (selectedSeats.Count == 0)
            return Fail("Vui lòng chọn ít nhất một ghế.");

        var showtime = await _db.Showtimes.AsNoTracking()
            .FirstOrDefaultAsync(x => x.MaSuatChieu == showtimeId, cancellationToken);
        if (showtime == null)
            return Fail("Không tìm thấy suất chiếu.");

        var seatsInRoom = await _db.Seats.AsNoTracking()
            .Where(x => x.MaPhong == showtime.MaPhong && selectedSeats.Contains(x.MaGhe))
            .ToListAsync(cancellationToken);
        if (seatsInRoom.Count != selectedSeats.Count)
            return Fail("Một hoặc nhiều ghế không thuộc suất chiếu này.");

        var ticketTotal = seatsInRoom.Sum(seat =>
            showtime.GiaVe + (string.Equals(seat.LoaiGhe, "VIP", StringComparison.OrdinalIgnoreCase) ? 30000m : 0m));
        var comboTotal = 0m;
        foreach (var item in ParseCombos(combos))
        {
            var combo = await _db.Combos.AsNoTracking()
                .FirstOrDefaultAsync(x => x.MaCombo == item.Id, cancellationToken);
            if (combo == null)
                return Fail("Không tìm thấy một hoặc nhiều combo đã chọn.");
            comboTotal += combo.Gia * item.Quantity;
        }

        var subtotal = ticketTotal + comboTotal;
        return await _voucherService.ValidateAsync(customerId, code, subtotal, cancellationToken);
    }

    private static IEnumerable<(string Id, int Quantity)> ParseCombos(string? combos)
    {
        if (string.IsNullOrWhiteSpace(combos))
            yield break;

        foreach (var item in combos.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            var parts = item.Split(':', 2, StringSplitOptions.TrimEntries);
            if (parts.Length != 2 || string.Equals(parts[0], "voucher", StringComparison.OrdinalIgnoreCase)
                || string.Equals(parts[0], "points", StringComparison.OrdinalIgnoreCase))
                continue;

            if (!int.TryParse(parts[1], out var quantity) || quantity <= 0)
                throw new ArgumentException("Số lượng combo không hợp lệ.");
            if (quantity > 20)
                throw new ArgumentException("Số lượng combo vượt quá giới hạn cho một lần đặt.");

            yield return (parts[0], quantity);
        }
    }

    private static VoucherValidationResult Fail(string message) => new()
    {
        Success = false,
        Message = message
    };
}
