using CinemaBD.Application.Interfaces;
using CinemaBD.Domain.Entities;
using CinemaBD.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CinemaBD.Infrastructure.Services;

public class AdminComboService : IAdminComboService
{
    private readonly AppDbContext _db;
    public AdminComboService(AppDbContext db) => _db = db;
    public async Task<List<Combo>> GetAllAsync(CancellationToken ct = default) => await _db.Combos.AsNoTracking().OrderBy(x => x.MaCombo).Select(x => new Combo { Id = x.MaCombo, Name = x.TenCombo, Price = x.Gia, Description = x.MoTa, ImageUrl = x.Anh }).ToListAsync(ct);
    public async Task<Combo?> GetByIdAsync(string id, CancellationToken ct = default) => await _db.Combos.AsNoTracking().Where(x => x.MaCombo == id).Select(x => new Combo { Id = x.MaCombo, Name = x.TenCombo, Price = x.Gia, Description = x.MoTa, ImageUrl = x.Anh }).FirstOrDefaultAsync(ct);
    public async Task<Combo> UpsertAsync(string? id, string name, decimal price, string? description, string? imageUrl, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Tên combo không được rỗng", nameof(name));
        if (price < 0) throw new ArgumentException("Giá combo không được âm", nameof(price));

        var comboId = string.IsNullOrWhiteSpace(id) ? await BuildComboIdAsync(ct) : id.Trim();
        var entity = await _db.Combos.FirstOrDefaultAsync(x => x.MaCombo == comboId, ct);
        if (entity == null) { entity = new LegacyCombo { MaCombo = comboId, TenCombo = name.Trim(), Gia = price, MoTa = description, Anh = imageUrl }; _db.Combos.Add(entity); }
        else { entity.TenCombo = name.Trim(); entity.Gia = price; entity.MoTa = description; entity.Anh = imageUrl; }
        await _db.SaveChangesAsync(ct);
        return new Combo { Id = entity.MaCombo, Name = entity.TenCombo, Price = entity.Gia, Description = entity.MoTa, ImageUrl = entity.Anh };
    }
    public async Task<bool> DeleteAsync(string id, CancellationToken ct = default)
    {
        var e = await _db.Combos.FindAsync([id], ct);
        if (e == null) return false;
        var isUsed = await _db.BookedCombos.AnyAsync(x => x.MaCombo == id, ct)
                     || await _db.InvoiceLineItems.AnyAsync(x => x.MaCombo == id, ct);
        if (isUsed)
        {
            e.TenCombo = e.TenCombo.StartsWith("[Ngưng bán]", StringComparison.OrdinalIgnoreCase) ? e.TenCombo : "[Ngưng bán] " + e.TenCombo;
            await _db.SaveChangesAsync(ct);
            return true;
        }

        _db.Combos.Remove(e);
        await _db.SaveChangesAsync(ct);
        return true;
    }

    private async Task<string> BuildComboIdAsync(CancellationToken ct)
    {
        var ids = await _db.Combos.AsNoTracking().Select(x => x.MaCombo).ToListAsync(ct);
        var max = ids.Select(id => id.Length > 2 && id.StartsWith("CB", StringComparison.OrdinalIgnoreCase) && int.TryParse(id[2..], out var n) ? n : 0).DefaultIfEmpty(0).Max();
        for (var i = max + 1; i <= 999; i++)
        {
            var id = $"CB{i:00}";
            if (!ids.Contains(id, StringComparer.OrdinalIgnoreCase)) return id;
        }
        return "CB" + DateTime.Now.ToString("yyMMddHHmmss");
    }
}

public class AdminInvoiceService : IAdminInvoiceService
{
    private const string CheckedInStatus = "CheckedIn";
    private readonly AppDbContext _db;
    public AdminInvoiceService(AppDbContext db) => _db = db;
    public async Task<List<InvoiceDetail>> GetAllAsync(CancellationToken ct = default)
    {
        var invoices = await _db.Invoices.AsNoTracking()
            .OrderByDescending(x => x.NgayThanhToan)
            .ToListAsync(ct);

        var result = new List<InvoiceDetail>();
        foreach (var invoice in invoices)
            result.Add(await BuildInvoiceDetailAsync(invoice, includeLineItems: false, ct));

        return result;
    }
    public async Task<InvoiceDetail?> GetByIdAsync(string id, CancellationToken ct = default)
    {
        var invoice = await _db.Invoices.AsNoTracking().FirstOrDefaultAsync(x => x.MaHD == id, ct);
        if (invoice == null) return null;
        return await BuildInvoiceDetailAsync(invoice, includeLineItems: true, ct);
    }

    public async Task<InvoiceSyncReport> GetSyncReportAsync(CancellationToken ct = default)
    {
        return await BuildSyncReportAsync(false, ct);
    }

    public async Task<InvoiceSyncReport> SyncAsync(CancellationToken ct = default)
    {
        return await BuildSyncReportAsync(true, ct);
    }

    public async Task<CheckInResult> CheckInAsync(string qrText, CancellationToken ct = default)
    {
        var (txnRef, invoiceId, ticketId) = ParseCheckInPayload(qrText);
        if (string.IsNullOrWhiteSpace(txnRef) && string.IsNullOrWhiteSpace(invoiceId) && string.IsNullOrWhiteSpace(ticketId))
            return new CheckInResult { Success = false, Message = "Mã QR không đúng định dạng." };

        var ticket = string.IsNullOrWhiteSpace(ticketId)
            ? null
            : await _db.Tickets.FirstOrDefaultAsync(x => x.MaVe == ticketId, ct);

        // QR sau thanh toán cũ đang nhét danh sách ghế (VD: A1,A2) vào vị trí TicketId,
        // trong khi check-in chuẩn cần mã vé VE... Cho phép map token ghế -> vé theo mã giao dịch.
        if (ticket == null
            && !string.IsNullOrWhiteSpace(ticketId)
            && !ticketId.StartsWith("VE", StringComparison.OrdinalIgnoreCase)
            && !string.IsNullOrWhiteSpace(txnRef))
        {
            var seatTokens = ticketId.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
            if (seatTokens.Length == 1)
            {
                ticket = await _db.Tickets.FirstOrDefaultAsync(x =>
                    x.GatewayTxnRef == txnRef && x.MaGhe == seatTokens[0], ct);
            }
        }

        var invoice = await _db.Invoices.FirstOrDefaultAsync(x =>
            (!string.IsNullOrWhiteSpace(invoiceId) && x.MaHD == invoiceId) ||
            (!string.IsNullOrWhiteSpace(txnRef) && x.GatewayTxnRef == txnRef) ||
            (ticket != null && !string.IsNullOrWhiteSpace(ticket.GatewayTxnRef) && x.GatewayTxnRef == ticket.GatewayTxnRef), ct);

        if (ticket == null && !string.IsNullOrWhiteSpace(ticketId))
            return new CheckInResult { Success = false, Message = "Không tìm thấy vé.", TicketId = ticketId };

        if (invoice == null)
            return new CheckInResult { Success = false, Message = "Không tìm thấy hóa đơn/vé." };

        if (ticket == null && !string.IsNullOrWhiteSpace(invoiceId))
        {
            var ticketIds = await _db.InvoiceLineItems.AsNoTracking()
                .Where(x => x.MaHD == invoice.MaHD && x.MaVe != null)
                .Select(x => x.MaVe!)
                .ToListAsync(ct);
            if (ticketIds.Count == 1)
                ticket = await _db.Tickets.FirstOrDefaultAsync(x => x.MaVe == ticketIds[0], ct);
        }

        var detail = await BuildCheckInResultAsync(invoice, ticket, ct);

        if (!string.Equals(invoice.TrangThai, "Thành công", StringComparison.OrdinalIgnoreCase)
            && !string.Equals(invoice.TrangThai, "Đã thanh toán", StringComparison.OrdinalIgnoreCase)
            && !string.Equals(invoice.TrangThai, "Paid", StringComparison.OrdinalIgnoreCase)
            && !string.Equals(invoice.TrangThai, "Success", StringComparison.OrdinalIgnoreCase))
        {
            detail.Success = false;
            detail.Message = $"Vé chưa thanh toán hợp lệ. Trạng thái hiện tại: {invoice.TrangThai}";
            return detail;
        }

        if (ticket == null)
        {
            detail.Success = false;
            detail.Message = "QR hóa đơn không đủ để check-in từng vé. Vui lòng quét QR trên vé cụ thể.";
            return detail;
        }

        if (IsCheckedInStatus(ticket.TrangThai))
        {
            detail.Success = false;
            detail.Message = $"Vé {ticket.MaVe} đã check-in.";
            detail.IsCheckedIn = true;
            detail.CheckedInAt = ticket.NgayDat;
            return detail;
        }

        ticket.TrangThai = "CheckedIn";
        invoice.GhiChu = AppendNote(invoice.GhiChu, $"Check-in vé {ticket.MaVe} lúc {DateTime.Now:dd/MM/yyyy HH:mm}");

        var allInvoiceTickets = await _db.Tickets
            .Where(x => x.GatewayTxnRef == invoice.GatewayTxnRef)
            .ToListAsync(ct);
        if (allInvoiceTickets.Count > 0 && allInvoiceTickets.All(x => x.MaVe == ticket.MaVe || IsCheckedInStatus(x.TrangThai)))
        {
            invoice.GhiChu = AppendNote(invoice.GhiChu, $"Đã check-in đủ hóa đơn lúc {DateTime.Now:dd/MM/yyyy HH:mm}");
        }

        await _db.SaveChangesAsync(ct);

        detail.Success = true;
        detail.Message = $"Check-in vé {ticket.MaVe} thành công.";
        detail.IsCheckedIn = true;
        detail.CheckedInAt = DateTime.Now;
        return detail;
    }

    private async Task<InvoiceSyncReport> BuildSyncReportAsync(bool fix, CancellationToken ct)
    {
        var report = new InvoiceSyncReport();
        var invoices = await _db.Invoices.OrderByDescending(x => x.NgayThanhToan).ToListAsync(ct);
        report.TotalInvoices = invoices.Count;

        var duplicateTxnRefs = invoices
            .Where(x => !string.IsNullOrWhiteSpace(x.GatewayTxnRef))
            .GroupBy(x => x.GatewayTxnRef)
            .Where(g => g.Count() > 1)
            .Select(g => g.Key)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (var invoice in invoices)
        {
            var issue = await BuildSyncIssueAsync(invoice, duplicateTxnRefs, ct);

            if (fix)
            {
                var fixedAny = await FixInvoiceAsync(invoice, issue, ct);
                if (fixedAny)
                {
                    issue = await BuildSyncIssueAsync(invoice, duplicateTxnRefs, ct);
                    report.FixedTicketLines += issue.TicketLineCount;
                    report.FixedComboLines += issue.ComboLineCount;
                }

                if (NormalizeStatus(invoice))
                    report.NormalizedInvoices++;

                var payment = await _db.Payments.FirstOrDefaultAsync(x => x.GatewayTxnRef == invoice.GatewayTxnRef, ct);
                if (payment != null && NormalizeStatus(payment))
                    report.NormalizedPayments++;
            }

            if (issue.HasIssue)
                report.Items.Add(issue);
        }

        if (fix)
        {
            await _db.SaveChangesAsync(ct);
            report.Items.Clear();
            foreach (var invoice in await _db.Invoices.AsNoTracking().OrderByDescending(x => x.NgayThanhToan).ToListAsync(ct))
            {
                var issue = await BuildSyncIssueAsync(invoice, duplicateTxnRefs, ct);
                if (issue.HasIssue) report.Items.Add(issue);
            }
        }

        report.IssueCount = report.Items.Count;
        return report;
    }

    private async Task<InvoiceSyncIssue> BuildSyncIssueAsync(LegacyInvoice invoice, HashSet<string> duplicateTxnRefs, CancellationToken ct)
    {
        var lines = await _db.InvoiceLineItems.AsNoTracking().Where(x => x.MaHD == invoice.MaHD).ToListAsync(ct);
        var tickets = await _db.Tickets.AsNoTracking().Where(x => x.GatewayTxnRef == invoice.GatewayTxnRef).ToListAsync(ct);
        var combos = await _db.BookedCombos.AsNoTracking().Where(x => x.GatewayTxnRef == invoice.GatewayTxnRef).ToListAsync(ct);
        var paymentExists = await _db.Payments.AsNoTracking().AnyAsync(x => x.GatewayTxnRef == invoice.GatewayTxnRef, ct);

        var hasMovieShowtime = false;
        var firstTicket = tickets.FirstOrDefault();
        if (firstTicket != null)
        {
            hasMovieShowtime = await (from s in _db.Showtimes.AsNoTracking()
                                      join m in _db.Movies.AsNoTracking() on s.MaPhim equals m.MaPhim
                                      where s.MaSuatChieu == firstTicket.MaSuatChieu
                                      select s.MaSuatChieu).AnyAsync(ct);
        }

        var issue = new InvoiceSyncIssue
        {
            InvoiceId = invoice.MaHD,
            TransactionRef = invoice.GatewayTxnRef,
            CustomerId = invoice.MaKH,
            Status = invoice.TrangThai,
            PaymentDate = invoice.NgayThanhToan,
            LineCount = lines.Count,
            TicketLineCount = lines.Count(x => !string.IsNullOrWhiteSpace(x.MaVe)),
            ComboLineCount = lines.Count(x => !string.IsNullOrWhiteSpace(x.MaCombo)),
            TicketCount = tickets.Count,
            ComboBookingCount = combos.Count,
            MissingPayment = !paymentExists,
            MissingTicketLines = tickets.Count > 0 && !tickets.All(t => lines.Any(l => l.MaVe == t.MaVe)),
            MissingComboLines = combos.Count > 0 && !combos.All(c => lines.Any(l => l.MaCombo == c.MaCombo)),
            MissingMovieOrShowtime = tickets.Count > 0 && !hasMovieShowtime,
            DuplicateTransactionRef = !string.IsNullOrWhiteSpace(invoice.GatewayTxnRef) && duplicateTxnRefs.Contains(invoice.GatewayTxnRef)
        };

        if (issue.MissingPayment) issue.Issues.Add("Thiếu thanh toán");
        if (issue.MissingTicketLines) issue.Issues.Add("Thiếu dòng vé trong chi tiết hóa đơn");
        if (issue.MissingComboLines) issue.Issues.Add("Thiếu dòng combo trong chi tiết hóa đơn");
        if (issue.TicketCount == 0) issue.Issues.Add("Không có vé liên kết theo mã giao dịch");
        if (issue.MissingMovieOrShowtime) issue.Issues.Add("Thiếu phim/suất chiếu liên kết");
        if (issue.DuplicateTransactionRef) issue.Issues.Add("Trùng mã giao dịch GatewayTxnRef");

        return issue;
    }

    private async Task<bool> FixInvoiceAsync(LegacyInvoice invoice, InvoiceSyncIssue issue, CancellationToken ct)
    {
        var fixedAny = false;
        var tickets = await _db.Tickets.Where(x => x.GatewayTxnRef == invoice.GatewayTxnRef).ToListAsync(ct);
        var existingTicketIds = await _db.InvoiceLineItems.Where(x => x.MaHD == invoice.MaHD && x.MaVe != null).Select(x => x.MaVe!).ToListAsync(ct);
        foreach (var ticket in tickets.Where(t => !existingTicketIds.Contains(t.MaVe)))
        {
            _db.InvoiceLineItems.Add(new LegacyInvoiceLineItem { MaHD = invoice.MaHD, LoaiDong = "Ve", MaVe = ticket.MaVe, TenDong = $"Ghế {ticket.MaGhe}", SoLuong = 1, DonGia = ticket.GiaVe });
            fixedAny = true;
        }

        var combos = await _db.BookedCombos.AsNoTracking().Where(x => x.GatewayTxnRef == invoice.GatewayTxnRef).ToListAsync(ct);
        var comboIds = combos.Select(x => x.MaCombo).Distinct().ToList();
        var comboNames = await _db.Combos.AsNoTracking().Where(x => comboIds.Contains(x.MaCombo)).ToDictionaryAsync(x => x.MaCombo, x => x.TenCombo, ct);
        var existingComboIds = await _db.InvoiceLineItems.Where(x => x.MaHD == invoice.MaHD && x.MaCombo != null).Select(x => x.MaCombo!).ToListAsync(ct);
        foreach (var combo in combos.Where(c => !existingComboIds.Contains(c.MaCombo)))
        {
            _db.InvoiceLineItems.Add(new LegacyInvoiceLineItem { MaHD = invoice.MaHD, LoaiDong = "Combo", MaCombo = combo.MaCombo, TenDong = comboNames.GetValueOrDefault(combo.MaCombo, combo.MaCombo), SoLuong = combo.SoLuong, DonGia = combo.SoLuong <= 0 ? combo.Gia : combo.Gia / combo.SoLuong });
            fixedAny = true;
        }

        return fixedAny;
    }

    private static bool NormalizeStatus(LegacyInvoice invoice)
    {
        var normalized = NormalizePaidStatus(invoice.TrangThai);
        if (normalized == invoice.TrangThai) return false;
        invoice.TrangThai = normalized;
        return true;
    }

    private static bool NormalizeStatus(LegacyPayment payment)
    {
        var normalized = NormalizePaidStatus(payment.TrangThai);
        if (normalized == payment.TrangThai) return false;
        payment.TrangThai = normalized;
        return true;
    }

    private static string NormalizePaidStatus(string? status)
    {
        if (string.IsNullOrWhiteSpace(status)) return "Pending";
        var value = status.Trim();
        if (value.Equals("Thành công", StringComparison.OrdinalIgnoreCase) || value.Equals("Đã thanh toán", StringComparison.OrdinalIgnoreCase) || value.Equals("Success", StringComparison.OrdinalIgnoreCase)) return "Paid";
        if (value.Equals("Thất bại", StringComparison.OrdinalIgnoreCase)) return "Failed";
        if (value.Equals("Đã hủy", StringComparison.OrdinalIgnoreCase) || value.Equals("Da huy", StringComparison.OrdinalIgnoreCase)) return "Cancelled";
        return value;
    }

    private static bool IsCheckedInStatus(string? status)
        => string.Equals(status, CheckedInStatus, StringComparison.OrdinalIgnoreCase);

    private static string AppendNote(string? current, string note)
        => string.IsNullOrWhiteSpace(current) ? note : current + " | " + note;

    private static (string? TxnRef, string? InvoiceId, string? TicketId) ParseCheckInPayload(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return (null, null, null);

        raw = raw.Trim();

        if (Uri.TryCreate(raw, UriKind.Absolute, out var uri))
        {
            var queryValues = ParseQuery(uri.Query);
            if (queryValues.TryGetValue("qr", out var qr))
                raw = qr.Trim();
            else if (queryValues.TryGetValue("qrText", out var qrText))
                raw = qrText.Trim();
            else if (queryValues.TryGetValue("txnRef", out var txnRef) || queryValues.TryGetValue("transactionRef", out txnRef))
                return (txnRef.Trim(), queryValues.TryGetValue("invoiceId", out var invoiceId) ? invoiceId.Trim() : null, queryValues.TryGetValue("ticketId", out var ticketId) ? ticketId.Trim() : null);
        }

        raw = Uri.UnescapeDataString(raw).Trim();

        if (raw.StartsWith("CinemaBD|CHECKIN|", StringComparison.OrdinalIgnoreCase))
        {
            var parts = raw.Split('|', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
            var txnRef = parts.Length > 2 ? parts[2] : null;
            var invoiceId = parts.Length > 3 ? parts[3] : null;
            var ticketId = parts.Length > 4 ? parts[4] : null;
            return (txnRef, invoiceId, ticketId);
        }

        if (raw.Contains('|'))
        {
            var parts = raw.Split('|', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
            var txnRef = parts.FirstOrDefault(x => x.StartsWith("TXN", StringComparison.OrdinalIgnoreCase));
            var invoiceId = parts.FirstOrDefault(x => x.StartsWith("HD", StringComparison.OrdinalIgnoreCase));
            var ticketId = parts.FirstOrDefault(x => x.StartsWith("VE", StringComparison.OrdinalIgnoreCase));
            return (txnRef, invoiceId, ticketId);
        }

        if (raw.StartsWith("HD", StringComparison.OrdinalIgnoreCase)) return (null, raw, null);
        if (raw.StartsWith("VE", StringComparison.OrdinalIgnoreCase)) return (null, null, raw);
        return (raw, null, null);
    }

    private static Dictionary<string, string> ParseQuery(string query)
    {
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        if (string.IsNullOrWhiteSpace(query)) return result;

        foreach (var pair in query.TrimStart('?').Split('&', StringSplitOptions.RemoveEmptyEntries))
        {
            var parts = pair.Split('=', 2);
            var key = Uri.UnescapeDataString(parts[0]).Trim();
            var value = parts.Length > 1 ? Uri.UnescapeDataString(parts[1]).Trim() : string.Empty;
            if (!string.IsNullOrWhiteSpace(key)) result[key] = value;
        }
        return result;
    }

    private async Task<InvoiceDetail> BuildInvoiceDetailAsync(LegacyInvoice invoice, bool includeLineItems, CancellationToken ct)
    {
        var customer = await _db.Customers.AsNoTracking().FirstOrDefaultAsync(x => x.MaKH == invoice.MaKH, ct);

        var lines = includeLineItems
            ? await _db.InvoiceLineItems.AsNoTracking()
                .Where(x => x.MaHD == invoice.MaHD)
                .OrderBy(x => x.MaCT)
                .Select(x => new InvoiceLineItem
                {
                    Id = x.MaCT,
                    InvoiceId = x.MaHD,
                    LineType = x.LoaiDong ?? string.Empty,
                    TicketId = x.MaVe,
                    ComboId = x.MaCombo,
                    ItemName = x.TenDong ?? string.Empty,
                    Quantity = x.SoLuong,
                    UnitPrice = x.DonGia
                })
                .ToListAsync(ct)
            : new List<InvoiceLineItem>();

        var lineTicketIds = lines
            .Select(x => x.TicketId)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct()
            .ToList();

        var tickets = await _db.Tickets.AsNoTracking()
            .Where(x => x.GatewayTxnRef == invoice.GatewayTxnRef || lineTicketIds.Contains(x.MaVe))
            .OrderBy(x => x.MaVe)
            .ToListAsync(ct);

        foreach (var line in lines.Where(x => !string.IsNullOrWhiteSpace(x.TicketId)))
        {
            var ticketLine = tickets.FirstOrDefault(x => x.MaVe == line.TicketId);
            if (ticketLine != null)
            {
                line.IsCheckedIn = IsCheckedInStatus(ticketLine.TrangThai);
                line.CheckedInAt = line.IsCheckedIn ? ticketLine.NgayDat : null;
            }
        }

        if (includeLineItems && lines.Count == 0 && tickets.Count > 0)
        {
            lines = tickets.Select(x => new InvoiceLineItem
            {
                InvoiceId = invoice.MaHD,
                LineType = "Ve",
                TicketId = x.MaVe,
                ItemName = $"Ghế {x.MaGhe}",
                Quantity = 1,
                UnitPrice = x.GiaVe,
                IsCheckedIn = IsCheckedInStatus(x.TrangThai),
                CheckedInAt = IsCheckedInStatus(x.TrangThai) ? x.NgayDat : null
            }).ToList();
        }

        var ticket = tickets.FirstOrDefault();
        LegacyShowtime? showtime = null;
        LegacyMovie? movie = null;
        if (ticket != null)
        {
            showtime = await _db.Showtimes.AsNoTracking().FirstOrDefaultAsync(x => x.MaSuatChieu == ticket.MaSuatChieu, ct);
            if (showtime != null)
                movie = await _db.Movies.AsNoTracking().FirstOrDefaultAsync(x => x.MaPhim == showtime.MaPhim, ct);
        }

        return new InvoiceDetail
        {
            InvoiceId = invoice.MaHD,
            CustomerName = customer?.HoTen ?? invoice.MaKH,
            MovieTitle = movie?.TenPhim,
            ShowDate = showtime?.NgayChieu,
            StartTime = showtime?.GioBatDau,
            TotalAmount = invoice.TongTien,
            Status = invoice.TrangThai,
            PaymentDate = invoice.NgayThanhToan,
            Note = invoice.GhiChu,
            TransactionRef = invoice.GatewayTxnRef,
            IsCheckedIn = invoice.GhiChu?.Contains("check-in", StringComparison.OrdinalIgnoreCase) == true,
            CheckedInAt = null,
            LineItems = lines
        };
    }

    private async Task<CheckInResult> BuildCheckInResultAsync(LegacyInvoice invoice, LegacyTicket? ticket, CancellationToken ct)
    {
        var detail = await BuildInvoiceDetailAsync(invoice, includeLineItems: false, ct);
        return new CheckInResult
        {
            InvoiceId = detail.InvoiceId,
            TransactionRef = detail.TransactionRef,
            TicketId = ticket?.MaVe,
            SeatId = ticket?.MaGhe,
            CustomerName = detail.CustomerName,
            MovieTitle = detail.MovieTitle,
            ShowDate = detail.ShowDate,
            StartTime = detail.StartTime,
            TotalAmount = detail.TotalAmount,
            Status = detail.Status,
            IsCheckedIn = ticket != null ? IsCheckedInStatus(ticket.TrangThai) : detail.IsCheckedIn,
            CheckedInAt = ticket != null && IsCheckedInStatus(ticket.TrangThai) ? ticket.NgayDat : detail.CheckedInAt
        };
    }
}

public class AdminStatisticsService : IAdminStatisticsService
{
    private readonly AppDbContext _db;
    public AdminStatisticsService(AppDbContext db) => _db = db;
    public async Task<RevenueStatistics> GetRevenueAsync(int? year = null, CancellationToken ct = default)
    {
        var selectedYear = year ?? DateTime.Now.Year;
        var paidInvoiceStatuses = new[] { "Đã thanh toán", "Thành công", "Paid", "Success", "CheckedIn" };
        var paidTicketStatuses = new[] { "Paid", "Success", "Thành công", "Đã thanh toán", "CheckedIn" };
        var paidPaymentStatuses = new[] { "Thành công", "Đã thanh toán", "Paid", "Success" };

        var invoices = await _db.Invoices.AsNoTracking()
            .Where(x => x.NgayThanhToan.Year == selectedYear && paidInvoiceStatuses.Contains(x.TrangThai))
            .Select(x => new { x.GatewayTxnRef, x.TongTien, x.NgayThanhToan, x.TrangThai })
            .ToListAsync(ct);

        var txnRefs = invoices.Select(x => x.GatewayTxnRef).Where(x => !string.IsNullOrWhiteSpace(x)).Distinct().ToList();
        var payments = await _db.Payments.AsNoTracking()
            .Where(x => x.GatewayTxnRef != null && txnRefs.Contains(x.GatewayTxnRef) && paidPaymentStatuses.Contains(x.TrangThai))
            .Select(x => new { x.GatewayTxnRef, Method = x.PaymentGateway ?? x.HinhThuc ?? "Khác", x.SoTien })
            .ToListAsync(ct);

        var ticketRows = await (from t in _db.Tickets.AsNoTracking()
                                join s in _db.Showtimes.AsNoTracking() on t.MaSuatChieu equals s.MaSuatChieu
                                join m in _db.Movies.AsNoTracking() on s.MaPhim equals m.MaPhim
                                where t.GatewayTxnRef != null && txnRefs.Contains(t.GatewayTxnRef) && paidTicketStatuses.Contains(t.TrangThai)
                                select new { t.GatewayTxnRef, Movie = m.TenPhim, t.GiaVe, t.TrangThai })
            .ToListAsync(ct);

        var comboRows = await (from c in _db.BookedCombos.AsNoTracking()
                               join combo in _db.Combos.AsNoTracking() on c.MaCombo equals combo.MaCombo into comboJoin
                               from combo in comboJoin.DefaultIfEmpty()
                               where txnRefs.Contains(c.GatewayTxnRef)
                               select new { Name = combo != null ? combo.TenCombo : c.MaCombo, c.Gia })
            .ToListAsync(ct);

        var customerRows = await (from h in _db.Invoices.AsNoTracking()
                                  join kh in _db.Customers.AsNoTracking() on h.MaKH equals kh.MaKH into khJoin
                                  from kh in khJoin.DefaultIfEmpty()
                                  where h.NgayThanhToan.Year == selectedYear && paidInvoiceStatuses.Contains(h.TrangThai)
                                  select new { Customer = kh != null ? kh.HoTen : h.MaKH, h.TongTien })
            .ToListAsync(ct);

        var now = DateTime.Now;
        var totalRevenue = invoices.Sum(x => x.TongTien);
        var vietnameseWeekdays = new[] { "CN", "T2", "T3", "T4", "T5", "T6", "T7" };

        return new RevenueStatistics
        {
            TotalRevenue = totalRevenue,
            TotalOrders = invoices.Count,
            TotalTickets = ticketRows.Count,
            CheckedInTickets = ticketRows.Count(x => x.TrangThai == "CheckedIn"),
            AverageOrderValue = invoices.Count == 0 ? 0 : Math.Round(totalRevenue / invoices.Count, 0),
            TodayRevenue = invoices.Where(x => x.NgayThanhToan.Date == now.Date).Sum(x => x.TongTien),
            CurrentMonthRevenue = invoices.Where(x => x.NgayThanhToan.Month == now.Month).Sum(x => x.TongTien),
            CurrentYearRevenue = totalRevenue,
            StatisticsDate = now,
            MonthlyData = Enumerable.Range(1, 12).Select(m => new RevenueDataPoint { Label = $"Tháng {m}", Value = invoices.Where(x => x.NgayThanhToan.Month == m).Sum(x => x.TongTien) }).ToList(),
            DailyData = Enumerable.Range(1, DateTime.DaysInMonth(selectedYear, now.Month)).Select(d => new RevenueDataPoint { Label = d.ToString(), Value = invoices.Where(x => x.NgayThanhToan.Month == now.Month && x.NgayThanhToan.Day == d).Sum(x => x.TongTien) }).ToList(),
            HourlyData = Enumerable.Range(0, 24).Select(h => new RevenueDataPoint { Label = $"{h:00}:00", Value = invoices.Where(x => x.NgayThanhToan.Date == now.Date && x.NgayThanhToan.Hour == h).Sum(x => x.TongTien) }).ToList(),
            WeekdayData = Enumerable.Range(0, 7).Select(d => new RevenueDataPoint { Label = vietnameseWeekdays[d], Value = invoices.Where(x => (int)x.NgayThanhToan.DayOfWeek == d).Sum(x => x.TongTien) }).ToList(),
            TopMovies = ticketRows.GroupBy(x => x.Movie).Select(g => new RevenueDataPoint { Label = g.Key, Value = g.Sum(x => x.GiaVe) }).OrderByDescending(x => x.Value).Take(8).ToList(),
            TopCombos = comboRows.GroupBy(x => x.Name).Select(g => new RevenueDataPoint { Label = g.Key, Value = g.Sum(x => x.Gia) }).OrderByDescending(x => x.Value).Take(8).ToList(),
            TopCustomers = customerRows.GroupBy(x => x.Customer).Select(g => new RevenueDataPoint { Label = g.Key, Value = g.Sum(x => x.TongTien) }).OrderByDescending(x => x.Value).Take(8).ToList(),
            PaymentMethods = payments.Any()
                ? payments.GroupBy(x => x.Method).Select(g => new PaymentMethodStat { Method = g.Key, OrderCount = g.Count(), Revenue = g.Sum(x => x.SoTien) }).OrderByDescending(x => x.Revenue).ToList()
                : new List<PaymentMethodStat> { new() { Method = "VNPAY/MOMO", OrderCount = invoices.Count, Revenue = totalRevenue } }
        };
    }
}

