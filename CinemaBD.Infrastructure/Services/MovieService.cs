using CinemaBD.Application.Interfaces;
using CinemaBD.Domain.Entities;
using CinemaBD.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CinemaBD.Infrastructure.Services;

public class MovieService : IMovieService
{
    private readonly AppDbContext _db;

    public MovieService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyCollection<Movie>> GetNowShowingAsync(CancellationToken cancellationToken = default)
    {
        var today = DateTime.Today;

        var data = await _db.Movies
            .AsNoTracking()
            .Where(p =>
                (p.TrangThai == null || p.TrangThai != "Inactive") &&
                _db.Showtimes.Any(s => s.MaPhim == p.MaPhim
                    && s.NgayChieu.Date >= today
                    && s.TrangThai != "Expired"
                    && s.TrangThai != "Cancelled"
                    && _db.Rooms.Any(r => r.MaPhong == s.MaPhong && (r.TrangThai == null || r.TrangThai == "Hoạt động" || r.TrangThai == "Hoat dong" || r.TrangThai == "Active"))))
            .OrderBy(p => p.TenPhim)
            .Select(p => new Movie
            {
                Id = p.MaPhim,
                Title = p.TenPhim,
                Genre = p.TheLoai,
                DurationMinutes = p.ThoiLuong,
                Description = p.MoTa,
                PosterUrl = p.AnhDaiDien,
                ReleaseDate = p.NgayKhoiChieu,
                EndDate = p.NgayKetThuc,
                Status = p.TrangThai
            })
            .ToListAsync(cancellationToken);

        return data;
    }

    public async Task<Movie?> GetByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        return await _db.Movies
            .AsNoTracking()
            .Where(p => p.MaPhim == id)
            .Select(p => new Movie
            {
                Id = p.MaPhim,
                Title = p.TenPhim,
                Genre = p.TheLoai,
                DurationMinutes = p.ThoiLuong,
                Description = p.MoTa,
                PosterUrl = p.AnhDaiDien,
                ReleaseDate = p.NgayKhoiChieu,
                EndDate = p.NgayKetThuc,
                Status = p.TrangThai,
                Director = p.DaoDien,
                Cast = p.DienVien,
                Country = p.Nguon,
                AgeRestriction = p.GioiHanTuoi,
                TrailerUrl = p.Trailer
            })
            .FirstOrDefaultAsync(cancellationToken);
    }
}

