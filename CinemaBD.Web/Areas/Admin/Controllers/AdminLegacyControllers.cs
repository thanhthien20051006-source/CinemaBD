using CinemaBD.Web.Core;
using Microsoft.AspNetCore.Mvc;

namespace CinemaBD.Web.Areas.Admin.Controllers;

public abstract class AdminLegacyListController : BaseAdminController
{
    protected readonly IAdminLegacyReadService LegacyService;
    protected AdminLegacyListController(IAdminLegacyReadService legacyService) => LegacyService = legacyService;
    protected IActionResult ListView(CinemaBD.Web.Models.AdminListPageViewModel model) => View("~/Areas/Admin/Views/Shared/AdminList.cshtml", model);
}

