using CinemaBD.Web.Core;
using CinemaBD.Web.Models;
using Microsoft.AspNetCore.Mvc;

namespace CinemaBD.Web.Controllers.Api;

[ApiController]
[Route("api/movies")]
public class MoviesApiController : ControllerBase
{
    private readonly IBookingCoreService _booking;

    public MoviesApiController(IBookingCoreService booking)
    {
        _booking = booking;
    }

    [HttpGet]
    public async Task<IActionResult> GetMovies(CancellationToken cancellationToken)
    {
        var data = await _booking.GetMoviesAsync(cancellationToken);
        return Ok(new ApiResponse<IReadOnlyList<MovieViewModel>> { Success = true, Message = "OK", Data = data });
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetMovieById(string id, CancellationToken cancellationToken)
    {
        var data = await _booking.GetMovieByIdAsync(id, cancellationToken);
        if (data is null)
            return NotFound(new ApiResponse<object> { Success = false, Message = "Không tìm thấy phim" });

        return Ok(new ApiResponse<MovieViewModel> { Success = true, Message = "OK", Data = data });
    }

    [HttpGet("{id}/showtimes")]
    public async Task<IActionResult> GetShowtimes(string id, [FromQuery] DateTime? date, CancellationToken cancellationToken)
    {
        var selectedDate = date?.Date ?? DateTime.Today;
        var data = await _booking.GetShowtimesAsync(id, selectedDate, cancellationToken);
        return Ok(new ApiResponse<IReadOnlyList<ShowtimeViewModel>> { Success = true, Message = "OK", Data = data });
    }
}

