using CinemaBD.Domain.Entities;

namespace CinemaBD.Application.Interfaces;

public interface IArticleService
{
    Task<List<Article>> GetPagedAsync(int page, int pageSize, CancellationToken ct = default);
    Task<Article?> GetByIdAsync(int id, CancellationToken ct = default);
    Task<int> GetTotalCountAsync(CancellationToken ct = default);
}
