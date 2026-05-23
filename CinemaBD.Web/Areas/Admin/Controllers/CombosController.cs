using CinemaBD.Web.Core;
using CinemaBD.Web.Models;
using Microsoft.AspNetCore.Mvc;

namespace CinemaBD.Web.Areas.Admin.Controllers;

[AdminPermission("dichvu", "combo")]
public class CombosController : AdminApiCrudController
{
    public CombosController(HttpClient http, IConfiguration configuration) : base(http, configuration) { }

    public async Task<IActionResult> Index(CancellationToken ct)
    {
        var data = await GetDataAsync<List<AdminComboEditViewModel>>("api/admin/combos", ct) ?? new();
        return View(data.OrderBy(x => x.Name).ToList());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(string id, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(id)) return RedirectToAction(nameof(Index));
        await SendAsync(HttpMethod.Delete, $"api/admin/combos/{Uri.EscapeDataString(id)}", null, ct);
        return RedirectToAction(nameof(Index));
    }
}
