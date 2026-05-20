using CinemaBD.Web.Core;
using CinemaBD.Web.Models;
using Microsoft.AspNetCore.Mvc;

namespace CinemaBD.Web.Controllers.Api;

[ApiController]
[Route("api/combos")]
public class CombosApiController : ControllerBase
{
    private readonly IBookingCoreService _booking;

    public CombosApiController(IBookingCoreService booking)
    {
        _booking = booking;
    }

    [HttpGet]
    public async Task<IActionResult> GetCombos(CancellationToken cancellationToken)
    {
        var data = await _booking.GetCombosAsync(cancellationToken);
        return Ok(new ApiResponse<IReadOnlyList<ComboViewModel>> { Success = true, Message = "OK", Data = data });
    }
}
