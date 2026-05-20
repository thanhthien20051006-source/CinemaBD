using CinemaBD.Api.Contracts.Common;
using CinemaBD.Api.Contracts.Movies;
using CinemaBD.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace CinemaBD.Api.Controllers;

[ApiController]
[Route("api/movies")]
public class MoviesController : ControllerBase
{
    private readonly IMovieService _movieService;
    private readonly IShowtimeService _showtimeService;

    public MoviesController(IMovieService movieService, IShowtimeService showtimeService)
    {
        _movieService = movieService;
        _showtimeService = showtimeService;
    }

    [HttpGet]
    public async Task<IActionResult> GetNowShowing(CancellationToken cancellationToken)
    {
        var data = await _movieService.GetNowShowingAsync(cancellationToken);
        var response = data.Select(p => new MovieResponse(
            p.Id,
            p.Title,
            p.Genre,
            p.DurationMinutes,
            p.Director,
            p.Cast,
            p.Country,
            p.AgeRestriction,
            p.Description,
            p.PosterUrl,
            p.TrailerUrl,
            p.ReleaseDate,
            p.EndDate,
            p.Status));

        return Ok(new ApiResponse<object>(true, "Lấy danh sách phim thành công", response));
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(string id, CancellationToken cancellationToken)
    {
        var p = await _movieService.GetByIdAsync(id, cancellationToken);
        if (p == null)
            return NotFound(new ApiResponse<object>(false, "Không tìm thấy phim", null));

        var response = new MovieResponse(
            p.Id,
            p.Title,
            p.Genre,
            p.DurationMinutes,
            p.Director,
            p.Cast,
            p.Country,
            p.AgeRestriction,
            p.Description,
            p.PosterUrl,
            p.TrailerUrl,
            p.ReleaseDate,
            p.EndDate,
            p.Status);

        return Ok(new ApiResponse<object>(true, "Lấy chi tiết phim thành công", response));
    }

    [HttpGet("{id}/showtimes")]
    public async Task<IActionResult> GetShowtimes(string id, [FromQuery] DateTime? date, CancellationToken cancellationToken)
    {
        var data = await _showtimeService.GetByMovieAsync(id, date, cancellationToken);
        var response = data.Select(s => new ShowtimeResponse(
            s.Id,
            s.ShowDate,
            s.StartTime,
            s.RoomId,
            s.RoomName,
            s.TicketPrice,
            s.TotalSeats,
            s.AvailableSeats,
            s.Status));

        return Ok(new ApiResponse<object>(true, "Lấy lịch chiếu thành công", response));
    }
}

