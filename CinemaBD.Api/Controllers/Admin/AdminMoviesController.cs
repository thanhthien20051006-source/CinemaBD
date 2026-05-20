using CinemaBD.Api.Contracts.Admin;
using CinemaBD.Api.Contracts.Common;
using CinemaBD.Api.Contracts.Movies;
using CinemaBD.Application.Interfaces;
using CinemaBD.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CinemaBD.Api.Controllers.Admin;

[ApiController]
[Authorize]
[Route("api/admin/movies")]
public class AdminMoviesController : ControllerBase
{
    private readonly IAdminMovieService _movieService;

    public AdminMoviesController(IAdminMovieService movieService)
    {
        _movieService = movieService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] string? search, CancellationToken cancellationToken)
    {
        var data = await _movieService.GetAllAsync(search, cancellationToken);
        var response = data.Select(p => new MovieResponse(p.Id, p.Title, p.Genre, p.DurationMinutes, p.Director, p.Cast, p.Country, p.AgeRestriction, p.Description, p.PosterUrl, p.TrailerUrl, p.ReleaseDate, p.EndDate, p.Status));
        return Ok(new ApiResponse<object>(true, "Lấy danh sách phim quản trị thành công", response));
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(string id, CancellationToken cancellationToken)
    {
        var data = await _movieService.GetByIdAsync(id, cancellationToken);
        if (data == null)
            return NotFound(new ApiResponse<object>(false, "Không tìm thấy phim", null));

        var response = new MovieResponse(data.Id, data.Title, data.Genre, data.DurationMinutes, data.Director, data.Cast, data.Country, data.AgeRestriction, data.Description, data.PosterUrl, data.TrailerUrl, data.ReleaseDate, data.EndDate, data.Status);
        return Ok(new ApiResponse<object>(true, "Lấy chi tiết phim thành công", response));
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] AdminMovieUpsertRequest request, CancellationToken cancellationToken)
    {
        var created = await _movieService.CreateAsync(new Movie
        {
            Title = request.Title,
            Genre = request.Genre,
            DurationMinutes = request.DurationMinutes,
            Director = request.Director,
            Cast = request.Cast,
            Country = request.Country,
            AgeRestriction = request.AgeRestriction,
            Description = request.Description,
            PosterUrl = request.PosterUrl,
            TrailerUrl = request.TrailerUrl,
            ReleaseDate = request.ReleaseDate,
            EndDate = request.EndDate,
            Status = request.Status
        }, cancellationToken);

        return Ok(new ApiResponse<object>(true, "Tạo phim thành công", created));
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(string id, [FromBody] AdminMovieUpsertRequest request, CancellationToken cancellationToken)
    {
        var updated = await _movieService.UpdateAsync(id, new Movie
        {
            Title = request.Title,
            Genre = request.Genre,
            DurationMinutes = request.DurationMinutes,
            Director = request.Director,
            Cast = request.Cast,
            Country = request.Country,
            AgeRestriction = request.AgeRestriction,
            Description = request.Description,
            PosterUrl = request.PosterUrl,
            TrailerUrl = request.TrailerUrl,
            ReleaseDate = request.ReleaseDate,
            EndDate = request.EndDate,
            Status = request.Status
        }, cancellationToken);

        if (updated == null)
            return NotFound(new ApiResponse<object>(false, "Không tìm thấy phim để cập nhật", null));

        return Ok(new ApiResponse<object>(true, "Cập nhật phim thành công", updated));
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(string id, CancellationToken cancellationToken)
    {
        var deleted = await _movieService.DeleteAsync(id, cancellationToken);
        if (!deleted)
            return NotFound(new ApiResponse<object>(false, "Không tìm thấy phim để xóa", null));

        return Ok(new ApiResponse<object>(true, "Xóa phim thành công", null));
    }
}

