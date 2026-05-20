using CinemaBD.Web.Core;
using CinemaBD.Web.Domain;
using Microsoft.AspNetCore.Mvc;

namespace CinemaBD.Web.Areas.Admin.Controllers;

public class CombosController : BaseAdminController
{
    private readonly IAdminComboCoreService _comboService;
    public CombosController(IAdminComboCoreService comboService) => _comboService = comboService;

    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var data = await _comboService.GetAllAsync(cancellationToken);
        return View(data);
    }

    [HttpGet]
    public IActionResult Create() => View(new Combo { Id = $"C{DateTime.Now:yyMMddHHmmss}" });

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Combo model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid) return View(model);
        await _comboService.CreateAsync(model, cancellationToken);
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(string id, CancellationToken cancellationToken)
    {
        await _comboService.DeleteAsync(id, cancellationToken);
        return RedirectToAction(nameof(Index));
    }
}

