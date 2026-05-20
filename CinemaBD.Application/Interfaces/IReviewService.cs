using CinemaBD.Domain.Entities;

namespace CinemaBD.Application.Interfaces;

public interface IReviewService
{
    Task<List<Review>> GetByMovieAsync(string movieId, CancellationToken ct = default);
    Task<Review> GetEligibilityAsync(string movieId, string customerId, CancellationToken ct = default);
    Task<Review> CreateAsync(string movieId, string customerId, string content, int rating, CancellationToken ct = default);
}
