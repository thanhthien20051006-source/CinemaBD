using CinemaBD.Application.Interfaces;
using CinemaBD.Domain.Entities;
using CinemaBD.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CinemaBD.Infrastructure.Services;

public class AdminCinemaService : IAdminCinemaService
{
    private readonly AppDbContext _db;
    public AdminCinemaService(AppDbContext db) => _db = db;

    public async Task<List<Cinema>> GetAllAsync(CancellationToken ct = default)
    {
        return await (from c in _db.Cinemas.AsNoTracking()
                      join r in _db.Rooms.AsNoTracking() on c.MaRap equals r.MaRap into rooms
                      orderby c.MaRap
                      select new Cinema
                      {
                          Id = c.MaRap,
                          Name = c.TenRap,
                          Address = c.DiaChi,
                          Phone = c.SoDienThoai,
                          Status = c.TrangThai,
                          RoomCount = rooms.Count()
                      }).ToListAsync(ct);
    }

    public async Task<Cinema?> GetByIdAsync(string id, CancellationToken ct = default)
    {
        return await (from c in _db.Cinemas.AsNoTracking()
                      where c.MaRap == id
                      join r in _db.Rooms.AsNoTracking() on c.MaRap equals r.MaRap into rooms
                      select new Cinema
                      {
                          Id = c.MaRap,
                          Name = c.TenRap,
                          Address = c.DiaChi,
                          Phone = c.SoDienThoai,
                          Status = c.TrangThai,
                          RoomCount = rooms.Count()
                      }).FirstOrDefaultAsync(ct);
    }

    public async Task<Cinema> UpsertAsync(string? id, string name, string? address, string? phone, string? status, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Tên rạp không được rỗng", nameof(name));
        var entity = !string.IsNullOrWhiteSpace(id) ? await _db.Cinemas.FirstOrDefaultAsync(x => x.MaRap == id, ct) : null;
        if (entity == null)
        {
            entity = new LegacyCinema
            {
                MaRap = string.IsNullOrWhiteSpace(id) ? $"RAP{DateTime.Now:yyMMddHHmmss}" : id.Trim(),
                TenRap = name.Trim(),
                DiaChi = address,
                SoDienThoai = phone,
                TrangThai = string.IsNullOrWhiteSpace(status) ? "Hoạt động" : status
            };
            _db.Cinemas.Add(entity);
        }
        else
        {
            entity.TenRap = name.Trim();
            entity.DiaChi = address;
            entity.SoDienThoai = phone;
            entity.TrangThai = string.IsNullOrWhiteSpace(status) ? entity.TrangThai : status;
        }

        await _db.SaveChangesAsync(ct);
        return new Cinema { Id = entity.MaRap, Name = entity.TenRap, Address = entity.DiaChi, Phone = entity.SoDienThoai, Status = entity.TrangThai };
    }

    public async Task<bool> ToggleStatusAsync(string id, CancellationToken ct = default)
    {
        var cinema = await _db.Cinemas.FirstOrDefaultAsync(x => x.MaRap == id, ct);
        if (cinema == null) return false;
        cinema.TrangThai = IsActive(cinema.TrangThai) ? "Ngưng hoạt động" : "Hoạt động";
        await _db.SaveChangesAsync(ct);
        return true;
    }

    private static bool IsActive(string? status) => string.Equals(status, "Hoạt động", StringComparison.OrdinalIgnoreCase) || string.Equals(status, "Active", StringComparison.OrdinalIgnoreCase);
}

public class AdminRoomService : IAdminRoomService
{
    private readonly AppDbContext _db;
    public AdminRoomService(AppDbContext db) => _db = db;
    public async Task<List<Room>> GetAllAsync(string? cinemaId = null, CancellationToken ct = default)
    {
        var query = from r in _db.Rooms.AsNoTracking()
                    join c in _db.Cinemas.AsNoTracking() on r.MaRap equals c.MaRap into cinemaJoin
                    from c in cinemaJoin.DefaultIfEmpty()
                    select new { r, c };
        if (!string.IsNullOrWhiteSpace(cinemaId)) query = query.Where(x => x.r.MaRap == cinemaId);
        return await query.OrderBy(x => x.r.MaRap).ThenBy(x => x.r.MaPhong).Select(x => new Room { Id = x.r.MaPhong, Name = x.r.TenPhong, CinemaId = x.r.MaRap, CinemaName = x.c != null ? x.c.TenRap : null, SeatCount = x.r.SoLuong, Status = x.r.TrangThai }).ToListAsync(ct);
    }
    public async Task<Room?> GetByIdAsync(string id, CancellationToken ct = default) => await (from r in _db.Rooms.AsNoTracking() where r.MaPhong == id join c in _db.Cinemas.AsNoTracking() on r.MaRap equals c.MaRap into cinemaJoin from c in cinemaJoin.DefaultIfEmpty() select new Room { Id = r.MaPhong, Name = r.TenPhong, CinemaId = r.MaRap, CinemaName = c != null ? c.TenRap : null, SeatCount = r.SoLuong, Status = r.TrangThai }).FirstOrDefaultAsync(ct);
    public async Task<bool> ToggleStatusAsync(string id, CancellationToken ct = default)
    {
        var room = await _db.Rooms.FirstOrDefaultAsync(x => x.MaPhong == id, ct);
        if (room == null) return false;
        room.TrangThai = string.Equals(room.TrangThai, "Hoạt động", StringComparison.OrdinalIgnoreCase) || string.Equals(room.TrangThai, "Active", StringComparison.OrdinalIgnoreCase) ? "Ngưng hoạt động" : "Hoạt động";
        await _db.SaveChangesAsync(ct);
        return true;
    }
}

public class AdminSeatService : IAdminSeatService
{
    private readonly AppDbContext _db;
    public AdminSeatService(AppDbContext db) => _db = db;
    public async Task<List<Seat>> GetAllAsync(string? roomId = null, string? search = null, CancellationToken ct = default)
    {
        var query = _db.Seats.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(roomId)) query = query.Where(x => x.MaPhong == roomId);
        if (!string.IsNullOrWhiteSpace(search)) query = query.Where(x => x.MaGhe.Contains(search));
        return await query.OrderBy(x => x.MaPhong).ThenBy(x => x.MaGhe).Select(x => new Seat { Id = x.MaGhe, RoomId = x.MaPhong, Row = x.MaGhe.Substring(0, 1), Column = x.MaGhe.Length > 1 ? x.MaGhe.Substring(1) : "", SeatType = x.LoaiGhe, IsBooked = x.TrangThai != "Trống", Price = 0 }).ToListAsync(ct);
    }
    public Task<List<Seat>> GetSeatMapAsync(string roomId, CancellationToken ct = default) => GetAllAsync(roomId, null, ct);
    public async Task<string?> ToggleStatusAsync(string seatId, CancellationToken ct = default)
    {
        var seat = await _db.Seats.FirstOrDefaultAsync(x => x.MaGhe == seatId, ct);
        if (seat == null) return null;
        seat.TrangThai = seat.TrangThai == "Khóa" ? "Trống" : "Khóa";
        await _db.SaveChangesAsync(ct);
        return seat.TrangThai;
    }
}

public class AdminGenreService : IAdminGenreService
{
    private readonly AppDbContext _db;
    public AdminGenreService(AppDbContext db) => _db = db;
    public async Task<List<Genre>> GetAllAsync(CancellationToken ct = default) => await _db.Genres.AsNoTracking().OrderBy(x => x.MaTL).Select(x => new Genre { Id = x.MaTL, Name = x.TenTheLoai, Description = x.MoTa, CreatedAt = x.CreatedAt, UpdatedAt = x.UpdatedAt }).ToListAsync(ct);
    public async Task<Genre?> GetByIdAsync(int id, CancellationToken ct = default) => await _db.Genres.AsNoTracking().Where(x => x.MaTL == id).Select(x => new Genre { Id = x.MaTL, Name = x.TenTheLoai, Description = x.MoTa, CreatedAt = x.CreatedAt, UpdatedAt = x.UpdatedAt }).FirstOrDefaultAsync(ct);
    public async Task<Genre> UpsertAsync(int id, string name, string? description, CancellationToken ct = default)
    {
        var entity = id > 0 ? await _db.Genres.FirstOrDefaultAsync(x => x.MaTL == id, ct) : null;
        if (entity == null)
        {
            entity = new LegacyGenre { TenTheLoai = name, MoTa = description, CreatedAt = DateTime.Now };
            _db.Genres.Add(entity);
        }
        else { entity.TenTheLoai = name; entity.MoTa = description; entity.UpdatedAt = DateTime.Now; }
        await _db.SaveChangesAsync(ct);
        return new Genre { Id = entity.MaTL, Name = entity.TenTheLoai, Description = entity.MoTa, CreatedAt = entity.CreatedAt, UpdatedAt = entity.UpdatedAt };
    }
    public async Task<bool> DeleteAsync(int id, CancellationToken ct = default) { var e = await _db.Genres.FindAsync([id], ct); if (e == null) return false; _db.Genres.Remove(e); await _db.SaveChangesAsync(ct); return true; }
}

public class AdminArticleService : IAdminArticleService
{
    private readonly AppDbContext _db;
    public AdminArticleService(AppDbContext db) => _db = db;
    public async Task<List<Article>> GetAllAsync(CancellationToken ct = default) => await _db.Articles.AsNoTracking().OrderByDescending(x => x.NgayDang).Select(x => new Article { Id = x.MaBV, Title = x.TieuDe, Summary = x.MoTa, Content = x.NoiDung, ImageUrl = x.Anh, PublishedAt = x.NgayDang }).ToListAsync(ct);
    public async Task<Article?> GetByIdAsync(int id, CancellationToken ct = default) => await _db.Articles.AsNoTracking().Where(x => x.MaBV == id).Select(x => new Article { Id = x.MaBV, Title = x.TieuDe, Summary = x.MoTa, Content = x.NoiDung, ImageUrl = x.Anh, PublishedAt = x.NgayDang }).FirstOrDefaultAsync(ct);
    public async Task<Article> CreateAsync(string title, string? summary, string? content, string? imageUrl, CancellationToken ct = default) { var e = new LegacyArticle { TieuDe = title, MoTa = summary, NoiDung = content, Anh = imageUrl, NgayDang = DateTime.Now }; _db.Articles.Add(e); await _db.SaveChangesAsync(ct); return new Article { Id = e.MaBV, Title = e.TieuDe, Summary = e.MoTa, Content = e.NoiDung, ImageUrl = e.Anh, PublishedAt = e.NgayDang }; }
    public async Task<Article> UpdateAsync(int id, string title, string? summary, string? content, string? imageUrl, CancellationToken ct = default) { var e = await _db.Articles.FirstAsync(x => x.MaBV == id, ct); e.TieuDe = title; e.MoTa = summary; e.NoiDung = content; e.Anh = imageUrl; await _db.SaveChangesAsync(ct); return new Article { Id = e.MaBV, Title = e.TieuDe, Summary = e.MoTa, Content = e.NoiDung, ImageUrl = e.Anh, PublishedAt = e.NgayDang }; }
    public async Task<bool> DeleteAsync(int id, CancellationToken ct = default) { var e = await _db.Articles.FindAsync([id], ct); if (e == null) return false; _db.Articles.Remove(e); await _db.SaveChangesAsync(ct); return true; }
}

public class AdminEventService : IAdminEventService
{
    private readonly AppDbContext _db;
    public AdminEventService(AppDbContext db) => _db = db;
    public async Task<List<Event>> GetAllAsync(CancellationToken ct = default) => await _db.Events.AsNoTracking().OrderByDescending(x => x.NgayBatDau).Select(x => new Event { Id = x.MaSK, Title = x.TieuDe, Description = x.MoTa, ImageUrl = x.Anh, StartDate = x.NgayBatDau, EndDate = x.NgayKetThuc }).ToListAsync(ct);
    public async Task<Event?> GetByIdAsync(int id, CancellationToken ct = default) => await _db.Events.AsNoTracking().Where(x => x.MaSK == id).Select(x => new Event { Id = x.MaSK, Title = x.TieuDe, Description = x.MoTa, ImageUrl = x.Anh, StartDate = x.NgayBatDau, EndDate = x.NgayKetThuc }).FirstOrDefaultAsync(ct);
    public async Task<Event> CreateAsync(string title, string? description, string? imageUrl, DateTime? startDate, DateTime? endDate, CancellationToken ct = default) { var e = new LegacyEvent { TieuDe = title, MoTa = description, Anh = imageUrl, NgayBatDau = startDate, NgayKetThuc = endDate }; _db.Events.Add(e); await _db.SaveChangesAsync(ct); return new Event { Id = e.MaSK, Title = e.TieuDe, Description = e.MoTa, ImageUrl = e.Anh, StartDate = e.NgayBatDau, EndDate = e.NgayKetThuc }; }
    public async Task<Event> UpdateAsync(int id, string title, string? description, string? imageUrl, DateTime? startDate, DateTime? endDate, CancellationToken ct = default) { var e = await _db.Events.FirstAsync(x => x.MaSK == id, ct); e.TieuDe = title; e.MoTa = description; e.Anh = imageUrl; e.NgayBatDau = startDate; e.NgayKetThuc = endDate; await _db.SaveChangesAsync(ct); return new Event { Id = e.MaSK, Title = e.TieuDe, Description = e.MoTa, ImageUrl = e.Anh, StartDate = e.NgayBatDau, EndDate = e.NgayKetThuc }; }
    public async Task<bool> DeleteAsync(int id, CancellationToken ct = default) { var e = await _db.Events.FindAsync([id], ct); if (e == null) return false; _db.Events.Remove(e); await _db.SaveChangesAsync(ct); return true; }
}

