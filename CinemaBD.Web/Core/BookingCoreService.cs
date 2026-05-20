using CinemaBD.Web.Data;
using CinemaBD.Web.Domain;
using CinemaBD.Web.Models;
using Microsoft.EntityFrameworkCore;

namespace CinemaBD.Web.Core;

public class BookingCoreService : IBookingCoreService
{
    private readonly CinemaDbContext _db;

    public BookingCoreService(CinemaDbContext db) => _db = db;

    public async Task<IReadOnlyList<MovieViewModel>> GetMoviesAsync(CancellationToken cancellationToken = default)
    {
        return await _db.Movies.Select(x => new MovieViewModel
        {
            Id = x.Id,
            Title = x.Title,
            Genre = x.Genre,
            DurationMinutes = x.DurationMinutes,
            Director = x.Director,
            Cast = x.Cast,
            Country = x.Country,
            AgeRestriction = x.AgeRestriction,
            Description = x.Description,
            PosterUrl = x.PosterUrl,
            TrailerUrl = x.TrailerUrl,
            ReleaseDate = x.ReleaseDate,
            EndDate = x.EndDate,
            Status = x.Status
        }).ToListAsync(cancellationToken);
    }

    public async Task<MovieViewModel?> GetMovieByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        return await _db.Movies.Where(x => x.Id == id).Select(x => new MovieViewModel
        {
            Id = x.Id,
            Title = x.Title,
            Genre = x.Genre,
            DurationMinutes = x.DurationMinutes,
            Director = x.Director,
            Cast = x.Cast,
            Country = x.Country,
            AgeRestriction = x.AgeRestriction,
            Description = x.Description,
            PosterUrl = x.PosterUrl,
            TrailerUrl = x.TrailerUrl,
            ReleaseDate = x.ReleaseDate,
            EndDate = x.EndDate,
            Status = x.Status
        }).FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ShowtimeViewModel>> GetShowtimesAsync(string movieId, DateTime date, CancellationToken cancellationToken = default)
    {
        return await _db.Showtimes
            .Where(x => x.MovieId == movieId && x.ShowDate.Date == date.Date)
            .OrderBy(x => x.StartTime)
            .Select(x => new ShowtimeViewModel
            {
                Id = x.Id,
                ShowDate = x.ShowDate,
                StartTime = x.StartTime,
                RoomId = x.RoomId,
                RoomName = x.RoomName,
                TicketPrice = x.TicketPrice
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<SeatViewModel>> GetSeatsAsync(string showtimeId, CancellationToken cancellationToken = default)
    {
        return await _db.Seats
            .Where(x => x.ShowtimeId == showtimeId)
            .OrderBy(x => x.Row)
            .ThenBy(x => x.Column)
            .Select(x => new SeatViewModel
            {
                Id = x.Id,
                Row = x.Row,
                Column = x.Column,
                SeatType = x.SeatType,
                IsBooked = x.IsBooked,
                Price = x.Price
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ComboViewModel>> GetCombosAsync(CancellationToken cancellationToken = default)
    {
        return await _db.Combos.Select(x => new ComboViewModel
        {
            Id = x.Id,
            Name = x.Name,
            Price = x.Price,
            Description = x.Description,
            ImageUrl = x.ImageUrl
        }).ToListAsync(cancellationToken);
    }

    public async Task<CheckoutResponse?> CheckoutAsync(string userId, CheckoutRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.ShowtimeId) || request.Seats.Count == 0) return null;

        var seats = await _db.Seats
            .Where(x => x.ShowtimeId == request.ShowtimeId && request.Seats.Contains(x.Id))
            .ToListAsync(cancellationToken);

        if (seats.Count != request.Seats.Count || seats.Any(x => x.IsBooked)) return null;

        foreach (var seat in seats) seat.IsBooked = true;

        var txn = $"TXN{DateTime.UtcNow:yyMMddHHmmssfff}";
        _db.Bookings.Add(new Booking
        {
            TxnRef = txn,
            UserId = userId,
            ShowtimeId = request.ShowtimeId,
            SeatsCsv = string.Join(",", request.Seats),
            CombosRaw = request.Combos,
            TotalAmount = request.TotalAmount,
            PaymentStatus = "Pending",
            CreatedAt = DateTime.UtcNow
        });

        await _db.SaveChangesAsync(cancellationToken);
        return new CheckoutResponse(txn, string.Empty, request.TotalAmount);
    }

    public async Task<bool> ConfirmPaymentAsync(string txnRef, string? responseCode, CancellationToken cancellationToken = default)
    {
        var booking = await _db.Bookings.FirstOrDefaultAsync(x => x.TxnRef == txnRef, cancellationToken);
        if (booking is null) return false;

        if (string.Equals(responseCode, "00", StringComparison.OrdinalIgnoreCase))
        {
            booking.PaymentStatus = "Paid";
            await _db.SaveChangesAsync(cancellationToken);
            return true;
        }

        booking.PaymentStatus = "Failed";
        var seatIds = (booking.SeatsCsv ?? string.Empty).Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var seats = await _db.Seats.Where(x => x.ShowtimeId == booking.ShowtimeId && seatIds.Contains(x.Id)).ToListAsync(cancellationToken);
        foreach (var seat in seats) seat.IsBooked = false;

        await _db.SaveChangesAsync(cancellationToken);
        return false;
    }

    public async Task<InvoiceViewModel?> GetInvoiceAsync(string txnRef, CancellationToken cancellationToken = default)
    {
        var booking = await _db.Bookings.FirstOrDefaultAsync(x => x.TxnRef == txnRef, cancellationToken);
        if (booking is null) return null;

        var showtime = await _db.Showtimes.FirstOrDefaultAsync(x => x.Id == booking.ShowtimeId, cancellationToken);
        if (showtime is null) return null;

        var movie = await _db.Movies.FirstOrDefaultAsync(x => x.Id == showtime.MovieId, cancellationToken);
        if (movie is null) return null;

        return new InvoiceViewModel
        {
            TransactionRef = booking.TxnRef,
            MovieTitle = movie.Title,
            ShowDate = showtime.ShowDate,
            StartTime = TimeSpan.TryParse(showtime.StartTime, out var t) ? t : null,
            RoomName = showtime.RoomName,
            Seats = booking.SeatsCsv.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList(),
            TotalAmount = booking.TotalAmount,
            PaymentStatus = booking.PaymentStatus
        };
    }
}
