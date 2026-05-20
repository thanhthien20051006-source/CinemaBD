using CinemaBD.Api.Contracts.Common;
using CinemaBD.Api.Contracts.Reviews;
using CinemaBD.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace CinemaBD.Api.Controllers;

[ApiController]
[Route("api/reviews")]
public class ReviewsController : ControllerBase
{
    private readonly IReviewService _reviewService;
    private readonly IAdminReviewService _adminReviewService;

    public ReviewsController(IReviewService reviewService, IAdminReviewService adminReviewService)
    {
        _reviewService = reviewService;
        _adminReviewService = adminReviewService;
    }

    [HttpGet("movie/{movieId}")]
    public async Task<IActionResult> GetByMovie(string movieId, CancellationToken cancellationToken)
    {
        var reviews = await _reviewService.GetByMovieAsync(movieId, cancellationToken);
        return Ok(new ApiResponse<object>(true, "Lấy đánh giá phim thành công", reviews.Select(ToResponse)));
    }

    [Authorize]
    [HttpGet("movie/{movieId}/eligibility")]
    public async Task<IActionResult> Eligibility(string movieId, CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(JwtRegisteredClaimNames.Sub) ?? User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId))
            return Unauthorized(new ApiResponse<object>(false, "Không xác định được người dùng", null));

        var result = await _reviewService.GetEligibilityAsync(movieId, userId, cancellationToken);
        return Ok(new ApiResponse<object>(result.CanReview, result.ReviewRuleMessage ?? "OK", ToResponse(result)));
    }

    [Authorize]
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateReviewRequest request, CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(JwtRegisteredClaimNames.Sub) ?? User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId))
            return Unauthorized(new ApiResponse<object>(false, "Không xác định được người dùng", null));

        if (string.IsNullOrWhiteSpace(request.Content))
            return BadRequest(new ApiResponse<object>(false, "Nội dung đánh giá không được rỗng", null));

        try
        {
            var review = await _reviewService.CreateAsync(request.MovieId, userId, request.Content, request.Rating, cancellationToken);
            return Ok(new ApiResponse<object>(true, "Gửi đánh giá thành công", ToResponse(review)));
        }
        catch (InvalidOperationException ex)
        {
            return Ok(new ApiResponse<object>(false, ex.Message, null));
        }
    }

    [Authorize]
    [HttpGet("admin")]
    public async Task<IActionResult> GetAllForAdmin(CancellationToken cancellationToken)
    {
        var reviews = await _adminReviewService.GetAllAsync(cancellationToken);
        return Ok(new ApiResponse<object>(true, "Lấy danh sách đánh giá thành công", reviews.Select(ToResponse)));
    }

    [Authorize]
    [HttpPost("admin/{id:int}/toggle-hidden")]
    public async Task<IActionResult> ToggleHidden(int id, CancellationToken cancellationToken)
    {
        var ok = await _adminReviewService.ToggleHiddenAsync(id, cancellationToken);
        if (!ok) return NotFound(new ApiResponse<object>(false, "Không tìm thấy đánh giá", null));
        return Ok(new ApiResponse<object>(true, "Cập nhật trạng thái đánh giá thành công", null));
    }

    [Authorize]
    [HttpDelete("admin/{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        var deleted = await _adminReviewService.DeleteAsync(id, cancellationToken);
        if (!deleted) return NotFound(new ApiResponse<object>(false, "Không tìm thấy đánh giá", null));
        return Ok(new ApiResponse<object>(true, "Xóa đánh giá thành công", null));
    }

    private static ReviewResponse ToResponse(CinemaBD.Domain.Entities.Review review) => new(
        review.Id,
        review.MovieId,
        review.CustomerId,
        review.Content,
        review.Rating,
        review.IsHidden,
        review.CanReview,
        review.ReviewRuleMessage,
        review.CreatedAt,
        review.MovieTitle,
        review.CustomerName);
}

