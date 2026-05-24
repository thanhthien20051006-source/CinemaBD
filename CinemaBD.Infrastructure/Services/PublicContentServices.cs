using CinemaBD.Application.Interfaces;
using CinemaBD.Domain.Entities;
using CinemaBD.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CinemaBD.Infrastructure.Services;

public class ArticleService : IArticleService
{
    private readonly AppDbContext _db;

    public ArticleService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<List<Article>> GetPagedAsync(int page, int pageSize, CancellationToken ct = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 50);

        return await _db.Articles.AsNoTracking()
            .OrderByDescending(x => x.NgayDang)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new Article
            {
                Id = x.MaBV,
                Title = x.TieuDe ?? string.Empty,
                Summary = x.MoTa,
                Content = x.NoiDung,
                ImageUrl = x.Anh,
                PublishedAt = x.NgayDang
            })
            .ToListAsync(ct);
    }

    public async Task<Article?> GetByIdAsync(int id, CancellationToken ct = default)
    {
        return await _db.Articles.AsNoTracking()
            .Where(x => x.MaBV == id)
            .Select(x => new Article
            {
                Id = x.MaBV,
                Title = x.TieuDe ?? string.Empty,
                Summary = x.MoTa,
                Content = x.NoiDung,
                ImageUrl = x.Anh,
                PublishedAt = x.NgayDang
            })
            .FirstOrDefaultAsync(ct);
    }

    public Task<int> GetTotalCountAsync(CancellationToken ct = default)
    {
        return _db.Articles.CountAsync(ct);
    }
}

public class EventService : IEventService
{
    private readonly AppDbContext _db;

    public EventService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<List<Event>> GetActiveAsync(int page, int pageSize, CancellationToken ct = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 50);
        var today = DateTime.Today;

        return await _db.Events.AsNoTracking()
            .Where(x => x.NgayKetThuc == null || x.NgayKetThuc >= today)
            .OrderBy(x => x.NgayBatDau ?? DateTime.MaxValue)
            .ThenByDescending(x => x.MaSK)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new Event
            {
                Id = x.MaSK,
                Title = x.TieuDe ?? string.Empty,
                Description = x.MoTa,
                ImageUrl = x.Anh,
                StartDate = x.NgayBatDau,
                EndDate = x.NgayKetThuc
            })
            .ToListAsync(ct);
    }

    public Task<int> GetActiveCountAsync(CancellationToken ct = default)
    {
        var today = DateTime.Today;
        return _db.Events.CountAsync(x => x.NgayKetThuc == null || x.NgayKetThuc >= today, ct);
    }
}
