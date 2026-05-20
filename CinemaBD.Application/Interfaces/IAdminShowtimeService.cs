using CinemaBD.Domain.Entities;

namespace CinemaBD.Application.Interfaces;

public interface IAdminShowtimeService
{
    Task<IReadOnlyCollection<ShowtimeDetail>> GetAllAsync(string? roomId, DateTime? date, CancellationToken cancellationToken = default);
    Task<ShowtimeDetail?> CreateAsync(string movieId, string roomId, DateTime showDate, string startTime, decimal ticketPrice, CancellationToken cancellationToken = default);
    Task<ShowtimeDetail?> UpdateAsync(string id, string movieId, string roomId, DateTime showDate, string startTime, decimal ticketPrice, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(string id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Đánh dấu "Expired" cho tất cả suất chiếu đã qua giờ chiếu.
    /// Trả về số lượng suất chiếu vừa được cập nhật.
    /// </summary>
    Task<int> ExpirePassedShowtimesAsync(CancellationToken cancellationToken = default);
}


