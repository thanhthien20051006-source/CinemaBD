using CinemaBD.Domain.Entities;

namespace CinemaBD.Application.Interfaces;

public interface IAdminShowtimeService
{
    Task<IReadOnlyCollection<ShowtimeDetail>> GetAllAsync(string? roomId, DateTime? date, CancellationToken cancellationToken = default);
    Task<ShowtimeDetail?> CreateAsync(string movieId, string roomId, DateTime showDate, string startTime, decimal ticketPrice, CancellationToken cancellationToken = default);
    Task<ShowtimeDetail?> UpdateAsync(string id, string movieId, string roomId, DateTime showDate, string startTime, decimal ticketPrice, string? status = null, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(string id, CancellationToken cancellationToken = default);
    Task<bool> CancelAsync(string id, CancellationToken cancellationToken = default);
    Task<ShowtimeGenerateResult> GenerateAsync(ShowtimeGenerateRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Danh dau "Expired" cho tat ca suat chieu da qua gio chieu.
    /// Tra ve so luong suat chieu vua duoc cap nhat.
    /// </summary>
    Task<int> ExpirePassedShowtimesAsync(CancellationToken cancellationToken = default);
}
