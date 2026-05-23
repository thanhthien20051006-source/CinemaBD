using CinemaBD.Application.Interfaces;
using CinemaBD.Domain.Entities;
using CinemaBD.Infrastructure.Payments;
using CinemaBD.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CinemaBD.Infrastructure.Services;

public class CustomerProfileService : ICustomerProfileService
{
    private readonly AppDbContext _db;

    public CustomerProfileService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<Customer?> GetProfileAsync(string customerId, CancellationToken ct = default)
    {
        var customer = await _db.Customers
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.MaKH == customerId, ct);

        if (customer == null)
            return null;

        return new Customer
        {
            Id = customer.MaKH,
            FullName = customer.HoTen,
            Username = customer.TKhoan,
            PasswordHash = customer.MatKhau,
            Email = customer.Email,
            PhoneNumber = customer.SDT,
            BirthDate = customer.NgaySinh
        };
    }

    public async Task<Customer?> UpdateProfileAsync(string customerId, CustomerProfileUpdate profile, CancellationToken ct = default)
    {
        var customer = await _db.Customers.FirstOrDefaultAsync(x => x.MaKH == customerId, ct);
        if (customer == null)
            return null;

        customer.HoTen = profile.FullName.Trim();
        customer.Email = string.IsNullOrWhiteSpace(profile.Email) ? null : profile.Email.Trim();
        customer.SDT = string.IsNullOrWhiteSpace(profile.PhoneNumber) ? null : profile.PhoneNumber.Trim();
        customer.NgaySinh = profile.BirthDate;

        await _db.SaveChangesAsync(ct);

        return new Customer
        {
            Id = customer.MaKH,
            FullName = customer.HoTen,
            Username = customer.TKhoan,
            PasswordHash = customer.MatKhau,
            Email = customer.Email,
            PhoneNumber = customer.SDT,
            BirthDate = customer.NgaySinh
        };
    }

    public async Task<List<CustomerHistory>> GetHistoryAsync(string customerId, CancellationToken ct = default)
    {
        var payments = await _db.Payments
            .AsNoTracking()
            .Where(p => PaymentStatuses.IsPaid(p.TrangThai))
            .Join(_db.Tickets.AsNoTracking(),
                payment => payment.GatewayTxnRef,
                ticket => ticket.GatewayTxnRef,
                (payment, ticket) => new { payment, ticket })
            .Where(x => x.ticket.MaKH == customerId)
            .Join(_db.Showtimes.AsNoTracking(),
                x => x.ticket.MaSuatChieu,
                showtime => showtime.MaSuatChieu,
                (x, showtime) => new { x.payment, x.ticket, showtime })
            .Join(_db.Movies.AsNoTracking(),
                x => x.showtime.MaPhim,
                movie => movie.MaPhim,
                (x, movie) => new { x.payment, x.ticket, x.showtime, movie })
            .ToListAsync(ct);

        return payments
            .GroupBy(x => x.payment.GatewayTxnRef)
            .Select(g => new CustomerHistory
            {
                InvoiceId = g.Key ?? string.Empty,
                MovieTitle = g.Select(x => x.movie.TenPhim).FirstOrDefault(),
                ShowDate = g.Select(x => x.showtime.NgayChieu).FirstOrDefault(),
                StartTime = g.Select(x => x.showtime.GioBatDau).FirstOrDefault(),
                TotalAmount = g.Select(x => x.payment.SoTien).FirstOrDefault(),
                Status = g.Select(x => x.payment.TrangThai).FirstOrDefault(),
                PaymentDate = g.Select(x => x.payment.PayDate ?? x.payment.NgayDat).FirstOrDefault(),
                TicketCount = g.Select(x => x.ticket.MaVe).Distinct().Count(),
                CheckedInCount = g.Count(x => x.ticket.TrangThai == "CheckedIn"),
                SeatIds = g.Select(x => x.ticket.MaGhe).OrderBy(x => x).ToList()
            })
            .OrderByDescending(x => x.PaymentDate)
            .ToList();
    }

    public async Task<decimal> GetTotalSpendingAsync(string customerId, int year, CancellationToken ct = default)
    {
        return await _db.Payments
            .AsNoTracking()
            .Where(p => PaymentStatuses.IsPaid(p.TrangThai) && (p.PayDate ?? p.NgayDat).Year == year)
            .Join(_db.Tickets.AsNoTracking(),
                payment => payment.GatewayTxnRef,
                ticket => ticket.GatewayTxnRef,
                (payment, ticket) => new { payment, ticket })
            .Where(x => x.ticket.MaKH == customerId)
            .Select(x => x.payment.GatewayTxnRef)
            .Distinct()
            .Join(_db.Payments.AsNoTracking(),
                txnRef => txnRef,
                payment => payment.GatewayTxnRef,
                (txnRef, payment) => payment.SoTien)
            .SumAsync(ct);
    }
}



