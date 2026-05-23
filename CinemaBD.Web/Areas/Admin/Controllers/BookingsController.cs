using CinemaBD.Web.Core;
using CinemaBD.Web.Models;
using Microsoft.AspNetCore.Mvc;

namespace CinemaBD.Web.Areas.Admin.Controllers;

[AdminPermission("chitietve", "ve", "booking")]
public class BookingsController : AdminApiCrudController
{
    public BookingsController(HttpClient http, IConfiguration configuration) : base(http, configuration) { }

    public async Task<IActionResult> Index(CancellationToken ct)
    {
        var invoices = await GetDataAsync<List<AdminInvoiceListItemViewModel>>("api/admin/invoices", ct) ?? new();
        return View(invoices.OrderByDescending(x => x.PaymentDate).ToList());
    }
}
