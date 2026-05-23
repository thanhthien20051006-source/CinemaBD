using CinemaBD.Application.Interfaces;
using CinemaBD.Domain.Entities;
using CinemaBD.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CinemaBD.Infrastructure.Services;

public class AdminDashboardService : IAdminDashboardService
{
    private readonly AppDbContext _db;

    public AdminDashboardService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<AdminDashboard> GetSummaryAsync(CancellationToken cancellationToken = default)
    {
        var paidInvoiceStatuses = new[] { "Đã thanh toán", "Thành công", "Paid", "Success", "CheckedIn" };
        var paidPaymentStatuses = new[] { "Thành công", "Đã thanh toán", "Paid", "Success" };

        var invoiceRevenue = await _db.Invoices.AsNoTracking()
            .Where(x => paidInvoiceStatuses.Contains(x.TrangThai))
            .SumAsync(x => (decimal?)x.TongTien, cancellationToken) ?? 0m;

        var paymentRevenue = await _db.Payments.AsNoTracking()
            .Where(x => paidPaymentStatuses.Contains(x.TrangThai))
            .SumAsync(x => (decimal?)x.SoTien, cancellationToken) ?? 0m;

        return new AdminDashboard
        {
            TotalMovies = await _db.Movies.AsNoTracking().CountAsync(cancellationToken),
            TotalShowtimes = await _db.Showtimes.AsNoTracking().CountAsync(cancellationToken),
            TotalCustomers = await _db.Customers.AsNoTracking().CountAsync(cancellationToken),
            TotalAdmins = await _db.Admins.AsNoTracking().CountAsync(x => x.IsActive, cancellationToken),
            TotalPaidRevenue = invoiceRevenue > 0 ? invoiceRevenue : paymentRevenue
        };
    }
}
