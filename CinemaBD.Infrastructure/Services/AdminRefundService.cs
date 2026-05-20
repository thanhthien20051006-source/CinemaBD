using CinemaBD.Application.Interfaces;
using CinemaBD.Domain.Entities;
using CinemaBD.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CinemaBD.Infrastructure.Services;

public class AdminRefundService : IAdminRefundService
{
    private readonly AppDbContext _db;

    public AdminRefundService(AppDbContext db) => _db = db;

    public async Task<List<RefundRequest>> GetAllAsync(string? status = null, CancellationToken ct = default)
    {
        var query = _db.RefundRequests.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(status))
            query = query.Where(x => x.Status == status);

        return await query
            .OrderByDescending(x => x.RequestedAt)
            .Select(x => new RefundRequest
            {
                Id = x.Id,
                InvoiceId = x.MaHD,
                TicketId = x.MaVe,
                CustomerId = x.MaKH,
                Reason = x.Reason,
                Status = x.Status,
                RequestedAt = x.RequestedAt,
                ApprovedAt = x.ApprovedAt,
                RejectedAt = x.RejectedAt,
                RefundAmount = x.RefundAmount,
                AdminNote = x.AdminNote
            })
            .ToListAsync(ct);
    }

    public async Task<RefundRequestResult> ApproveAsync(int id, string? adminNote, CancellationToken ct = default)
    {
        var request = await _db.RefundRequests.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (request == null) return Fail("Không tìm thấy yêu cầu hủy/hoàn tiền.");
        if (request.Status != "Pending") return Fail("Yêu cầu này đã được xử lý.");

        request.Status = "Approved";
        request.ApprovedAt = DateTime.Now;
        request.AdminNote = adminNote;

        var ticketQuery = _db.Tickets.Where(x => x.GatewayTxnRef == request.MaHD || x.MaVe == request.MaVe);
        if (!string.IsNullOrWhiteSpace(request.MaVe))
            ticketQuery = ticketQuery.Where(x => x.MaVe == request.MaVe);

        var tickets = await ticketQuery.ToListAsync(ct);
        foreach (var ticket in tickets)
            ticket.TrangThai = "Refunded";

        var invoice = await _db.Invoices.FirstOrDefaultAsync(x => x.MaHD == request.MaHD || x.GatewayTxnRef == request.MaHD, ct);
        if (invoice != null)
            invoice.TrangThai = "Refunded";

        var payments = await _db.Payments.Where(x => x.GatewayTxnRef == request.MaHD).ToListAsync(ct);
        foreach (var payment in payments)
            payment.TrangThai = "Refunded";

        await _db.SaveChangesAsync(ct);
        return Ok(request, "Đã duyệt hoàn tiền và cập nhật trạng thái vé/hóa đơn.");
    }

    public async Task<RefundRequestResult> RejectAsync(int id, string? adminNote, CancellationToken ct = default)
    {
        var request = await _db.RefundRequests.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (request == null) return Fail("Không tìm thấy yêu cầu hủy/hoàn tiền.");
        if (request.Status != "Pending") return Fail("Yêu cầu này đã được xử lý.");

        request.Status = "Rejected";
        request.RejectedAt = DateTime.Now;
        request.AdminNote = adminNote;
        await _db.SaveChangesAsync(ct);
        return Ok(request, "Đã từ chối yêu cầu hủy/hoàn tiền.");
    }

    private static RefundRequestResult Fail(string message) => new() { Success = false, Message = message };

    private static RefundRequestResult Ok(LegacyRefundRequest request, string message) => new()
    {
        Success = true,
        Message = message,
        Data = new RefundRequest
        {
            Id = request.Id,
            InvoiceId = request.MaHD,
            TicketId = request.MaVe,
            CustomerId = request.MaKH,
            Reason = request.Reason,
            Status = request.Status,
            RequestedAt = request.RequestedAt,
            ApprovedAt = request.ApprovedAt,
            RejectedAt = request.RejectedAt,
            RefundAmount = request.RefundAmount,
            AdminNote = request.AdminNote
        }
    };
}
