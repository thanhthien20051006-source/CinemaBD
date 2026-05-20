using CinemaBD.Web.Data;
using CinemaBD.Web.Domain;
using CinemaBD.Web.Models;
using Microsoft.EntityFrameworkCore;

namespace CinemaBD.Web.Core;

public class AuthCoreService : IAuthCoreService
{
    private readonly CinemaDbContext _db;

    public AuthCoreService(CinemaDbContext db) => _db = db;

    public async Task<AuthResponse?> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default)
    {
        var user = await _db.Users.FirstOrDefaultAsync(x => x.Username == request.Username, cancellationToken);
        if (user is null || user.PasswordHash != request.Password) return null;
        return new AuthResponse(user.Id, user.Username, user.FullName, user.Id);
    }

    public async Task<AuthResponse?> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default)
    {
        var existed = await _db.Users.AnyAsync(x => x.Username == request.Username, cancellationToken);
        if (existed) return null;

        var user = new User
        {
            Id = $"U{DateTime.UtcNow:yyyyMMddHHmmssfff}",
            FullName = request.FullName,
            Username = request.Username,
            PasswordHash = request.Password,
            Email = request.Email,
            PhoneNumber = request.PhoneNumber
        };

        _db.Users.Add(user);
        await _db.SaveChangesAsync(cancellationToken);
        return new AuthResponse(user.Id, user.Username, user.FullName, user.Id);
    }

    public async Task<UserProfileViewModel?> GetProfileAsync(string userId, CancellationToken cancellationToken = default)
    {
        var user = await _db.Users.FirstOrDefaultAsync(x => x.Id == userId, cancellationToken);
        if (user is null) return null;

        var totalSpentRaw = await _db.Bookings
            .Where(x => x.UserId == userId)
            .Select(x => (double?)x.TotalAmount)
            .SumAsync(cancellationToken) ?? 0d;

        return new UserProfileViewModel
        {
            FullName = user.FullName,
            Username = user.Username,
            Email = user.Email ?? string.Empty,
            PhoneNumber = user.PhoneNumber ?? string.Empty,
            TotalSpent = Convert.ToDecimal(totalSpentRaw)
        };
    }

    public async Task<IReadOnlyList<InvoiceHistoryItem>> GetHistoryAsync(string userId, CancellationToken cancellationToken = default)
    {
        var rows = await (from b in _db.Bookings
                          join s in _db.Showtimes on b.ShowtimeId equals s.Id
                          join m in _db.Movies on s.MovieId equals m.Id
                          where b.UserId == userId
                          orderby b.CreatedAt descending
                          select new { b.TxnRef, MovieTitle = m.Title, s.ShowDate, s.StartTime, b.TotalAmount, b.PaymentStatus, b.CreatedAt })
            .ToListAsync(cancellationToken);

        return rows.Select(x => new InvoiceHistoryItem(
            x.TxnRef, x.MovieTitle, x.ShowDate,
            TimeSpan.TryParse(x.StartTime, out var parsed) ? parsed : null,
            x.TotalAmount, x.PaymentStatus, x.CreatedAt)).ToList();
    }
}
