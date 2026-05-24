using CinemaBD.Api.Contracts.Common;
using CinemaBD.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace CinemaBD.Api.Controllers;

[ApiController]
[Route("api/events")]
public class EventsController : ControllerBase
{
    private readonly IEventService _eventService;

    public EventsController(IEventService eventService)
    {
        _eventService = eventService;
    }

    [HttpGet]
    public async Task<IActionResult> Get([FromQuery] int page = 1, [FromQuery] int pageSize = 9, CancellationToken ct = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 50);

        var events = await _eventService.GetActiveAsync(page, pageSize, ct);
        var total = await _eventService.GetActiveCountAsync(ct);

        return Ok(new ApiResponse<object>(true, "OK", new
        {
            Items = events,
            Page = page,
            PageSize = pageSize,
            Total = total,
            TotalPages = Math.Max(1, (int)Math.Ceiling(total / (double)pageSize))
        }));
    }
}
