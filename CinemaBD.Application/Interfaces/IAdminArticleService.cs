using CinemaBD.Domain.Entities;

namespace CinemaBD.Application.Interfaces;

public interface IAdminArticleService
{
    Task<List<Article>> GetAllAsync(CancellationToken ct = default);
    Task<Article?> GetByIdAsync(int id, CancellationToken ct = default);
    Task<Article> CreateAsync(string title, string? summary, string? content, string? imageUrl, CancellationToken ct = default);
    Task<Article> UpdateAsync(int id, string title, string? summary, string? content, string? imageUrl, CancellationToken ct = default);
    Task<bool> DeleteAsync(int id, CancellationToken ct = default);
}
