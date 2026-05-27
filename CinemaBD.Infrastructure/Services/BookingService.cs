using CinemaBD.Application.Interfaces;
using CinemaBD.Domain.Entities;
using CinemaBD.Infrastructure.Payments;
using CinemaBD.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using System.Data;

namespace CinemaBD.Infrastructure.Services;

public class BookingService : IBookingService
{
    private readonly AppDbContext _db;
    private static readonly TimeSpan SeatHoldDuration = TimeSpan.FromMinutes(5);
    private readonly VnPayUrlBuilder _vnPayUrlBuilder;
    private readonly ILoyaltyPointService _loyaltyPointService;
    private readonly IAdminVoucherService _voucherService;

    public BookingService(AppDbContext db, VnPayUrlBuilder vnPayUrlBuilder, ILoyaltyPointService loyaltyPointService, IAdminVoucherService voucherService)
    {
        _db = db;
        _vnPayUrlBuilder = vnPayUrlBuilder;
        _loyaltyPointService = loyaltyPointService;
        _voucherService = voucherService;
    }

    public async Task<SeatHoldResult> HoldSeatsAsync(string userId, string showtimeId, List<string> seats, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(userId))
            return new SeatHoldResult { Success = false, ShowtimeId = showtimeId, Message = "Cần đăng nhập để giữ ghế." };
        if (string.IsNullOrWhiteSpace(showtimeId) || seats.Count == 0)
            return new SeatHoldResult { Success = false, ShowtimeId = showtimeId, Message = "Thiếu suất chiếu hoặc ghế." };

        var now = DateTime.Now;
        var holdExpiredBefore = now.Subtract(SeatHoldDuration);
        await ExpireOldPendingTicketsAsync(holdExpiredBefore, cancellationToken);

        await using var transaction = await _db.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);
        var showtime = await _db.Showtimes.FirstOrDefaultAsync(s => s.MaSuatChieu == showtimeId, cancellationToken);
        if (showtime == null)
            return new SeatHoldResult { Success = false, ShowtimeId = showtimeId, Message = "Không tìm thấy suất chiếu." };

        var selectedSeats = seats.Select(s => s.Trim()).Where(s => !string.IsNullOrWhiteSpace(s)).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        var validSeats = await _db.Seats
            .Where(g => g.MaPhong == showtime.MaPhong && selectedSeats.Contains(g.MaGhe))
            .ToListAsync(cancellationToken);
        var invalidSeats = selectedSeats.Except(validSeats.Select(g => g.MaGhe), StringComparer.OrdinalIgnoreCase).ToList();
        if (invalidSeats.Any())
        {
            await transaction.RollbackAsync(cancellationToken);
            return new SeatHoldResult { Success = false, ShowtimeId = showtimeId, Message = $"Ghế {string.Join(", ", invalidSeats)} không tồn tại trong phòng chiếu." };
        }

        var unavailableSeats = await _db.Tickets
            .Where(v => v.MaSuatChieu == showtimeId
                && selectedSeats.Contains(v.MaGhe)
                && (v.TrangThai == "Paid" || (v.TrangThai == "Pending" && v.MaKH != userId && v.NgayDat > holdExpiredBefore)))
            .Select(v => v.MaGhe)
            .ToListAsync(cancellationToken);

        if (unavailableSeats.Any())
        {
            await transaction.RollbackAsync(cancellationToken);
            return new SeatHoldResult { Success = false, ShowtimeId = showtimeId, Message = $"Ghế {string.Join(", ", unavailableSeats)} đang được giữ hoặc đã được đặt.", UnavailableSeats = unavailableSeats };
        }

        var existingOwnHolds = await _db.Tickets
            .Where(v => v.MaSuatChieu == showtimeId && selectedSeats.Contains(v.MaGhe) && v.MaKH == userId && v.TrangThai == "Pending")
            .ToListAsync(cancellationToken);

        foreach (var ticket in existingOwnHolds)
            ticket.NgayDat = now;

        foreach (var seatCode in selectedSeats.Except(existingOwnHolds.Select(x => x.MaGhe), StringComparer.OrdinalIgnoreCase))
        {
            var seat = validSeats.First(g => string.Equals(g.MaGhe, seatCode, StringComparison.OrdinalIgnoreCase));
            var price = showtime.GiaVe + (((seat.LoaiGhe ?? string.Empty).ToUpper() == "VIP") ? 30000 : 0);
            _db.Tickets.Add(new LegacyTicket
            {
                MaVe = "VE" + Guid.NewGuid().ToString("N").Substring(0, 12).ToUpper(),
                MaKH = userId,
                MaSuatChieu = showtimeId,
                MaGhe = seatCode,
                GiaVe = price,
                TrangThai = "Pending",
                NgayDat = now,
                GatewayTxnRef = null
            });
        }

        await _db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return new SeatHoldResult { Success = true, ShowtimeId = showtimeId, Message = "Giữ ghế thành công.", HeldSeats = selectedSeats, ExpiresAt = now.Add(SeatHoldDuration) };
    }

    public async Task<SeatHoldResult> ReleaseSeatsAsync(string userId, string showtimeId, List<string> seats, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(userId) || string.IsNullOrWhiteSpace(showtimeId) || seats.Count == 0)
            return new SeatHoldResult { Success = false, ShowtimeId = showtimeId, Message = "Thiếu dữ liệu nhả ghế." };

        var selectedSeats = seats.Select(s => s.Trim()).Where(s => !string.IsNullOrWhiteSpace(s)).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        var ownPendingTickets = await _db.Tickets
            .Where(v => v.MaSuatChieu == showtimeId && selectedSeats.Contains(v.MaGhe) && v.MaKH == userId && v.TrangThai == "Pending" && v.GatewayTxnRef == null)
            .ToListAsync(cancellationToken);

        _db.Tickets.RemoveRange(ownPendingTickets);
        await _db.SaveChangesAsync(cancellationToken);

        return new SeatHoldResult { Success = true, ShowtimeId = showtimeId, Message = "Đã nhả ghế.", HeldSeats = ownPendingTickets.Select(x => x.MaGhe).ToList() };
    }

    public async Task<CheckoutResult> CreateCheckoutAsync(string userId, string showtimeId, List<string> seats, string? combos, decimal totalAmount, string? returnUrl = null, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(showtimeId))
            throw new ArgumentException("Thiếu mã suất chiếu.");
        if (seats == null || seats.Count == 0)
            throw new ArgumentException("Chưa chọn ghế.");
        if (string.IsNullOrWhiteSpace(userId))
            throw new ArgumentException("Thiếu mã khách hàng.");

        var now = DateTime.Now;
        var holdExpiredBefore = now.Subtract(SeatHoldDuration);

        await ExpireOldPendingTicketsAsync(holdExpiredBefore, cancellationToken);

        await using var transaction = await _db.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);

        var showtime = await _db.Showtimes.FirstOrDefaultAsync(s => s.MaSuatChieu == showtimeId, cancellationToken);
        if (showtime == null)
            throw new InvalidOperationException("Không tìm thấy suất chiếu.");

        var selectedSeats = seats.Select(s => s.Trim()).Where(s => !string.IsNullOrWhiteSpace(s)).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        var validSeats = await _db.Seats
            .Where(g => g.MaPhong == showtime.MaPhong && selectedSeats.Contains(g.MaGhe))
            .ToListAsync(cancellationToken);
        var invalidSeats = selectedSeats.Except(validSeats.Select(g => g.MaGhe), StringComparer.OrdinalIgnoreCase).ToList();
        if (invalidSeats.Any())
            throw new InvalidOperationException($"Ghế {string.Join(", ", invalidSeats)} không tồn tại trong phòng chiếu.");

        var unavailableSeats = await _db.Tickets
            .Where(v => v.MaSuatChieu == showtimeId
                && selectedSeats.Contains(v.MaGhe)
                && (v.TrangThai == "Paid" || (v.TrangThai == "Pending" && v.MaKH != userId && v.NgayDat > holdExpiredBefore)))
            .Select(v => v.MaGhe)
            .ToListAsync(cancellationToken);

        if (unavailableSeats.Any())
            throw new InvalidOperationException($"Ghế {string.Join(", ", unavailableSeats)} đang được giữ hoặc đã được đặt.");

        string txnRef = "TXN" + DateTime.UtcNow.ToString("yyMMddHHmmssfff");
        decimal ticketTotal = 0;

        foreach (var seatCode in selectedSeats)
        {
            var seat = validSeats.First(g => string.Equals(g.MaGhe, seatCode, StringComparison.OrdinalIgnoreCase));
            var price = showtime.GiaVe + (((seat.LoaiGhe ?? string.Empty).ToUpper() == "VIP") ? 30000 : 0);
            ticketTotal += price;

            var existingHold = await _db.Tickets.FirstOrDefaultAsync(v => v.MaSuatChieu == showtimeId && v.MaGhe == seatCode && v.MaKH == userId && v.TrangThai == "Pending", cancellationToken);
            if (existingHold != null)
            {
                existingHold.GatewayTxnRef = txnRef;
                existingHold.NgayDat = now;
                existingHold.GiaVe = price;
            }
            else
            {
                _db.Tickets.Add(new LegacyTicket
                {
                    MaVe = "VE" + Guid.NewGuid().ToString("N").Substring(0, 12).ToUpper(),
                    MaKH = userId,
                    MaSuatChieu = showtimeId,
                    MaGhe = seatCode,
                    GiaVe = price,
                    TrangThai = "Pending",
                    NgayDat = now,
                    GatewayTxnRef = txnRef
                });
            }
        }

        decimal comboTotal = 0;
        if (!string.IsNullOrWhiteSpace(combos))
        {
            var parsedCombos = combos
                .Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(x => x.Split(':'))
                .Where(parts => parts.Length == 2 && !string.Equals(parts[0].Trim(), "voucher", StringComparison.OrdinalIgnoreCase) && !string.Equals(parts[0].Trim(), "points", StringComparison.OrdinalIgnoreCase))
                .Select(parts => new { ComboId = parts[0].Trim(), QuantityText = parts[1].Trim() })
                .ToList();

            foreach (var item in parsedCombos)
            {
                if (!int.TryParse(item.QuantityText, out var quantity) || quantity <= 0)
                    continue;

                var combo = await _db.Combos.FirstOrDefaultAsync(c => c.MaCombo == item.ComboId, cancellationToken);
                if (combo == null)
                    continue;

                var comboAmount = combo.Gia * quantity;
                comboTotal += comboAmount;

                _db.BookedCombos.Add(new LegacyBookedCombo
                {
                    GatewayTxnRef = txnRef,
                    MaCombo = combo.MaCombo,
                    SoLuong = quantity,
                    Gia = comboAmount
                });
            }
        }

        var subtotal = ticketTotal + comboTotal;
        var discountAmount = await CalculateVoucherDiscountAsync(userId, combos, subtotal, cancellationToken);
        var loyaltyDiscount = await RedeemLoyaltyDiscountAsync(userId, combos, Math.Max(0, subtotal - discountAmount), cancellationToken);
        var payableAmount = Math.Max(0, subtotal - discountAmount - loyaltyDiscount);

        var voucherCode = ExtractComboValue(combos, "voucher");
        if (!string.IsNullOrWhiteSpace(voucherCode) && discountAmount > 0)
        {
            var normalizedCode = voucherCode.Trim().ToUpper();
            var voucher = await _db.Vouchers
                .Where(v => v.MaCode.ToUpper() == normalizedCode && (v.MaKH == userId || v.MaKH == "ALL" || v.MaKH == "*") && v.DaSuDung != true)
                .OrderBy(v => v.MaKH == userId ? 0 : 1)
                .FirstOrDefaultAsync(cancellationToken);
            if (voucher != null && voucher.MaKH != "ALL" && voucher.MaKH != "*")
            {
                voucher.DaSuDung = true;
                voucher.NgaySuDung = now;
                voucher.GatewayTxnRef = txnRef;
            }
        }

        _db.Payments.Add(new LegacyPayment
        {
            MaTT = "TT" + DateTime.Now.Ticks,
            SoTien = payableAmount,
            HinhThuc = "VNPAY",
            TrangThai = "Pending",
            GatewayTxnRef = txnRef,
            PaymentGateway = "VNPAY",
            NgayDat = DateTime.Now
        });

        await _db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        var paymentUrl = _vnPayUrlBuilder.Build(payableAmount, txnRef, ipAddress: "127.0.0.1", returnUrl);

        return new CheckoutResult
        {
            TransactionRef = txnRef,
            PaymentUrl = paymentUrl,
            TotalAmount = payableAmount
        };
    }



    private async Task<decimal> RedeemLoyaltyDiscountAsync(string userId, string? combos, decimal subtotal, CancellationToken cancellationToken)
    {
        var pointsText = ExtractComboValue(combos, "points");
        if (!int.TryParse(pointsText, out var points) || points <= 0 || subtotal <= 0)
            return 0;

        var entity = await _db.LoyaltyPoints.FirstOrDefaultAsync(x => x.MaKH == userId, cancellationToken);
        if (entity == null) return 0;

        var balance = entity.DiemThuong + entity.DiemCong - entity.DiemTru;
        if (balance <= 0) return 0;

        var actualPoints = Math.Min(points, balance);
        var discount = Math.Min(subtotal, actualPoints * 1000m);
        var usedPoints = (int)Math.Ceiling(discount / 1000m);
        if (usedPoints > 0)
            entity.DiemTru += usedPoints;
        return discount;
    }

    private async Task<decimal> CalculateVoucherDiscountAsync(string userId, string? combos, decimal subtotal, CancellationToken cancellationToken)
    {
        var voucherCode = ExtractComboValue(combos, "voucher");
        if (string.IsNullOrWhiteSpace(voucherCode) || subtotal <= 0)
            return 0;

        voucherCode = voucherCode.Trim();
        var normalizedCode = voucherCode.ToUpper();
        var today = DateTime.Today;
        var voucher = await _db.Vouchers
            .AsNoTracking()
            .Where(v => (v.MaKH == userId || v.MaKH == "ALL" || v.MaKH == "*") && v.NgayHetHan >= today && v.MaCode.ToUpper() == normalizedCode)
            .OrderBy(v => v.MaKH == userId ? 0 : 1)
            .FirstOrDefaultAsync(cancellationToken);

        if (voucher == null)
            return 0;

        if (voucher.DaSuDung == true)
            return 0;
        if ((voucher.DonToiThieu ?? 0) > subtotal)
            return 0;

        return AdminVoucherService.CalculateDiscount(voucher, subtotal);
    }

    private static string? ExtractComboValue(string? combos, string key)
    {
        if (string.IsNullOrWhiteSpace(combos))
            return null;

        foreach (var item in combos.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            var parts = item.Split(':', 2, StringSplitOptions.TrimEntries);
            if (parts.Length == 2 && string.Equals(parts[0], key, StringComparison.OrdinalIgnoreCase))
                return parts[1];
        }

        return null;
    }
    private async Task<int> ExpireOldPendingTicketsAsync(DateTime holdExpiredBefore, CancellationToken cancellationToken)
    {
        var expiredTickets = await _db.Tickets
            .Where(v => v.TrangThai == "Pending" && v.NgayDat <= holdExpiredBefore)
            .ToListAsync(cancellationToken);

        if (expiredTickets.Count == 0)
            return 0;

        var txnRefs = expiredTickets
            .Select(v => v.GatewayTxnRef)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct()
            .ToList();

        foreach (var ticket in expiredTickets)
            ticket.TrangThai = "Expired";

        var payments = await _db.Payments
            .Where(p => p.TrangThai == "Pending" && p.GatewayTxnRef != null && txnRefs.Contains(p.GatewayTxnRef))
            .ToListAsync(cancellationToken);

        foreach (var payment in payments)
            payment.TrangThai = "Expired";

        var changed = await _db.SaveChangesAsync(cancellationToken);

        foreach (var txnRef in txnRefs)
        {
            await _loyaltyPointService.RefundRedeemedPointsAsync(txnRef!, cancellationToken);
            await _voucherService.ReopenVoucherForFailedPaymentAsync(txnRef!, cancellationToken);
        }

        return changed;
    }
    public async Task<IReadOnlyCollection<string>> GetBookedSeatsAsync(string showtimeId, CancellationToken cancellationToken = default)
    {
        return await _db.Tickets
            .AsNoTracking()
            .Where(v => v.MaSuatChieu == showtimeId && (v.TrangThai == "Paid" || v.TrangThai == "CheckedIn" || (v.TrangThai == "Pending" && v.NgayDat > DateTime.Now.Subtract(SeatHoldDuration))))
            .Select(v => v.MaGhe)
            .ToListAsync(cancellationToken);
    }

    public async Task<RefundRequestResult> CreateRefundRequestAsync(string userId, string txnRef, string? ticketId, string reason, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(userId))
            return new RefundRequestResult { Success = false, Message = "Cần đăng nhập để yêu cầu hủy vé." };
        if (string.IsNullOrWhiteSpace(txnRef))
            return new RefundRequestResult { Success = false, Message = "Thiếu mã giao dịch/hóa đơn." };
        if (string.IsNullOrWhiteSpace(reason))
            return new RefundRequestResult { Success = false, Message = "Vui lòng nhập lý do hủy vé." };

        var invoice = await _db.Invoices.FirstOrDefaultAsync(x => (x.GatewayTxnRef == txnRef || x.MaHD == txnRef) && x.MaKH == userId, cancellationToken);
        var effectiveTxnRef = invoice?.GatewayTxnRef ?? txnRef;
        var ticketsQuery = _db.Tickets.Where(x => x.MaKH == userId && x.GatewayTxnRef == effectiveTxnRef && PaymentStatuses.IsPaid(x.TrangThai));
        if (!string.IsNullOrWhiteSpace(ticketId))
            ticketsQuery = ticketsQuery.Where(x => x.MaVe == ticketId);

        var tickets = await ticketsQuery.ToListAsync(cancellationToken);
        if (!tickets.Any())
            return new RefundRequestResult { Success = false, Message = "Không tìm thấy vé đã thanh toán để hủy." };
        if (tickets.Any(x => x.TrangThai == "CheckedIn"))
            return new RefundRequestResult { Success = false, Message = "Vé đã check-in nên không thể hủy." };

        var showtime = await _db.Showtimes.AsNoTracking().FirstOrDefaultAsync(x => x.MaSuatChieu == tickets.First().MaSuatChieu, cancellationToken);
        if (showtime != null)
        {
            var showDateTime = showtime.NgayChieu.Date.Add(showtime.GioBatDau);
            if (showDateTime <= DateTime.Now.AddHours(2))
                return new RefundRequestResult { Success = false, Message = "Chỉ được hủy vé trước giờ chiếu tối thiểu 2 giờ." };
        }

        var duplicate = await _db.RefundRequests.AnyAsync(x => x.MaKH == userId && (x.MaHD == effectiveTxnRef || x.MaHD == (invoice != null ? invoice.MaHD : effectiveTxnRef)) && x.Status == "Pending", cancellationToken);
        if (duplicate)
            return new RefundRequestResult { Success = false, Message = "Bạn đã gửi yêu cầu hủy/hoàn tiền cho hóa đơn này." };

        var refundAmount = tickets.Sum(x => x.GiaVe);
        var request = new LegacyRefundRequest
        {
            MaHD = invoice?.MaHD ?? effectiveTxnRef,
            MaVe = string.IsNullOrWhiteSpace(ticketId) ? null : ticketId,
            MaKH = userId,
            Reason = reason.Trim(),
            Status = "Pending",
            RequestedAt = DateTime.Now,
            RefundAmount = refundAmount
        };
        _db.RefundRequests.Add(request);
        foreach (var ticket in tickets)
            ticket.TrangThai = "CancelRequested";
        if (invoice != null)
            invoice.TrangThai = "CancelRequested";

        await _db.SaveChangesAsync(cancellationToken);
        return new RefundRequestResult
        {
            Success = true,
            Message = "Đã gửi yêu cầu hủy vé/hoàn tiền. Vui lòng chờ admin duyệt.",
            Data = new RefundRequest
            {
                Id = request.Id,
                InvoiceId = request.MaHD,
                TicketId = request.MaVe,
                CustomerId = request.MaKH,
                Reason = request.Reason,
                Status = request.Status,
                RequestedAt = request.RequestedAt,
                RefundAmount = request.RefundAmount
            }
        };
    }

    public async Task<Invoice?> GetInvoiceAsync(string txnRef, string? userId = null, bool isAdmin = false, CancellationToken cancellationToken = default)
    {
        var payment = await _db.Payments.AsNoTracking().FirstOrDefaultAsync(t => t.GatewayTxnRef == txnRef, cancellationToken);
        var invoice = await _db.Invoices.AsNoTracking().FirstOrDefaultAsync(h => h.GatewayTxnRef == txnRef, cancellationToken);
        if (payment == null && invoice == null)
            return null;

        var paymentStatus = payment?.TrangThai ?? invoice?.TrangThai ?? string.Empty;
        if (!PaymentStatuses.IsPaid(paymentStatus))
            return null;

        var lineItems = invoice == null
            ? new List<LegacyInvoiceLineItem>()
            : await _db.InvoiceLineItems.AsNoTracking().Where(x => x.MaHD == invoice.MaHD).ToListAsync(cancellationToken);

        var lineTicketIds = lineItems
            .Select(x => x.MaVe)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct()
            .ToList();

        var tickets = await _db.Tickets.AsNoTracking()
            .Where(v => v.GatewayTxnRef == txnRef || lineTicketIds.Contains(v.MaVe))
            .ToListAsync(cancellationToken);
        if (!tickets.Any())
            return null;

        if (!isAdmin && (string.IsNullOrWhiteSpace(userId) || !tickets.Any(t => t.MaKH == userId)))
            return null;

        var firstTicket = tickets.First();
        var showtime = await _db.Showtimes.AsNoTracking().FirstOrDefaultAsync(s => s.MaSuatChieu == firstTicket.MaSuatChieu, cancellationToken);
        if (showtime == null)
            return null;

        var movie = await _db.Movies.AsNoTracking().FirstOrDefaultAsync(p => p.MaPhim == showtime.MaPhim, cancellationToken);
        var room = await _db.Rooms.AsNoTracking().FirstOrDefaultAsync(r => r.MaPhong == showtime.MaPhong, cancellationToken);
        var combos = await _db.BookedCombos.AsNoTracking().Where(c => c.GatewayTxnRef == txnRef).ToListAsync(cancellationToken);
        if (!combos.Any() && lineItems.Any(x => !string.IsNullOrWhiteSpace(x.MaCombo)))
        {
            combos = lineItems
                .Where(x => !string.IsNullOrWhiteSpace(x.MaCombo))
                .Select(x => new LegacyBookedCombo { GatewayTxnRef = txnRef, MaCombo = x.MaCombo!, SoLuong = x.SoLuong, Gia = x.DonGia * x.SoLuong })
                .ToList();
        }
        var comboDefs = await _db.Combos.AsNoTracking().Where(c => combos.Select(x => x.MaCombo).Contains(c.MaCombo)).ToListAsync(cancellationToken);

        return new Invoice
        {
            TransactionRef = txnRef,
            PaymentId = payment?.MaTT ?? invoice?.MaHD ?? string.Empty,
            PaymentStatus = paymentStatus,
            TotalAmount = payment?.SoTien ?? invoice?.TongTien ?? 0,
            MovieId = movie?.MaPhim,
            MovieTitle = movie?.TenPhim,
            MoviePosterUrl = movie?.AnhDaiDien,
            ShowDate = showtime.NgayChieu,
            StartTime = showtime.GioBatDau,
            RoomName = room?.TenPhong,
            Seats = tickets.Select(v => v.MaGhe).OrderBy(x => x).ToList(),
            Tickets = tickets
                .OrderBy(v => v.MaGhe)
                .Select(v => new InvoiceTicket
                {
                    TicketId = v.MaVe,
                    SeatId = v.MaGhe,
                    Price = v.GiaVe,
                    Status = v.TrangThai,
                    IsCheckedIn = v.TrangThai == "CheckedIn",
                    CheckedInAt = v.TrangThai == "CheckedIn" ? v.NgayDat : null
                })
                .ToList(),
            Combos = combos.Select(c =>
            {
                var comboName = comboDefs.FirstOrDefault(x => x.MaCombo == c.MaCombo)?.TenCombo ?? c.MaCombo;
                return $"{comboName} x{c.SoLuong}";
            }).ToList()
        };
    }
}







