using CinemaBD.Application.Interfaces;
using CinemaBD.Domain.Entities;
using CinemaBD.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CinemaBD.Infrastructure.Services;

public class AdminMovieService : IAdminMovieService
{
    private readonly AppDbContext _db;

    public AdminMovieService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyCollection<Movie>> GetAllAsync(string? search, CancellationToken cancellationToken = default)
    {
        var query = _db.Movies.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(p => p.TenPhim.Contains(search) || (p.TheLoai != null && p.TheLoai.Contains(search)));
        }

        return await query.OrderBy(p => p.TenPhim)
            .Select(p => new Movie
            {
                Id = p.MaPhim,
                Title = p.TenPhim,
                Genre = p.TheLoai,
                DurationMinutes = p.ThoiLuong,
                Director = p.DaoDien,
                Cast = p.DienVien,
                Country = p.Nguon,
                AgeRestriction = p.GioiHanTuoi,
                Description = p.MoTa,
                PosterUrl = p.AnhDaiDien,
                TrailerUrl = p.Trailer,
                ReleaseDate = p.NgayKhoiChieu,
                EndDate = p.NgayKetThuc,
                Status = p.TrangThai
            }).ToListAsync(cancellationToken);
    }

    public async Task<Movie?> GetByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        return await _db.Movies.AsNoTracking().Where(p => p.MaPhim == id)
            .Select(p => new Movie
            {
                Id = p.MaPhim,
                Title = p.TenPhim,
                Genre = p.TheLoai,
                DurationMinutes = p.ThoiLuong,
                Director = p.DaoDien,
                Cast = p.DienVien,
                Country = p.Nguon,
                AgeRestriction = p.GioiHanTuoi,
                Description = p.MoTa,
                PosterUrl = p.AnhDaiDien,
                TrailerUrl = p.Trailer,
                ReleaseDate = p.NgayKhoiChieu,
                EndDate = p.NgayKetThuc,
                Status = p.TrangThai
            }).FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<Movie> CreateAsync(Movie movie, CancellationToken cancellationToken = default)
    {
        var last = await _db.Movies.OrderByDescending(p => p.MaPhim).FirstOrDefaultAsync(cancellationToken);
        var number = 1;
        if (last != null && last.MaPhim.Length > 1 && int.TryParse(last.MaPhim.Substring(1), out var n))
            number = n + 1;

        var entity = new LegacyMovie
        {
            MaPhim = "P" + number.ToString("D3"),
            TenPhim = movie.Title,
            TheLoai = movie.Genre,
            ThoiLuong = movie.DurationMinutes,
            DaoDien = movie.Director,
            DienVien = movie.Cast,
            Nguon = movie.Country,
            GioiHanTuoi = movie.AgeRestriction,
            MoTa = movie.Description,
            AnhDaiDien = movie.PosterUrl,
            Trailer = movie.TrailerUrl,
            NgayKhoiChieu = movie.ReleaseDate,
            NgayKetThuc = movie.EndDate,
            TrangThai = string.IsNullOrWhiteSpace(movie.Status) ? "Hoạt động" : movie.Status
        };

        _db.Movies.Add(entity);
        await _db.SaveChangesAsync(cancellationToken);
        movie.Id = entity.MaPhim;
        movie.Status = entity.TrangThai;
        return movie;
    }

    public async Task<Movie?> UpdateAsync(string id, Movie movie, CancellationToken cancellationToken = default)
    {
        var entity = await _db.Movies.FirstOrDefaultAsync(p => p.MaPhim == id, cancellationToken);
        if (entity == null)
            return null;

        entity.TenPhim = movie.Title;
        entity.TheLoai = movie.Genre;
        entity.ThoiLuong = movie.DurationMinutes;
        entity.DaoDien = movie.Director;
        entity.DienVien = movie.Cast;
        entity.Nguon = movie.Country;
        entity.GioiHanTuoi = movie.AgeRestriction;
        entity.MoTa = movie.Description;
        entity.AnhDaiDien = movie.PosterUrl;
        entity.Trailer = movie.TrailerUrl;
        entity.NgayKhoiChieu = movie.ReleaseDate;
        entity.NgayKetThuc = movie.EndDate;
        entity.TrangThai = movie.Status;

        await _db.SaveChangesAsync(cancellationToken);
        movie.Id = entity.MaPhim;
        return movie;
    }

    public async Task<bool> DeleteAsync(string id, CancellationToken cancellationToken = default)
    {
        var entity = await _db.Movies.FirstOrDefaultAsync(p => p.MaPhim == id, cancellationToken);
        if (entity == null)
            return false;

        _db.Movies.Remove(entity);
        await _db.SaveChangesAsync(cancellationToken);
        return true;
    }
}



