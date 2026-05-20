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
        return new AdminDashboard
        {
            TotalMovies = await _db.Movies.CountAsync(cancellationToken),
            TotalShowtimes = await _db.Showtimes.CountAsync(cancellationToken),
            TotalCustomers = await _db.Customers.CountAsync(cancellationToken),
            TotalAdmins = await _db.Admins.CountAsync(cancellationToken),
            TotalPaidRevenue = await _db.Payments.Where(p => p.TrangThai == "Thành công").SumAsync(p => (decimal?)p.SoTien, cancellationToken) ?? 0m
        };
    }
}



