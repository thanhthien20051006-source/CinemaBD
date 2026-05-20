using CinemaBD.Web.Core;
using Microsoft.AspNetCore.Mvc;

namespace CinemaBD.Web.Areas.Admin.Controllers;

[AdminPermission("chitietve", "ve", "booking")]
public class BookingsController : BaseAdminController
{
    private readonly IAdminBookingCoreService _bookingService;
    public BookingsController(IAdminBookingCoreService bookingService) => _bookingService = bookingService;

    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var data = await _bookingService.GetAllAsync(cancellationToken);
        return View(data);
    }
}

