using CinemaBD.Web.Core;
using CinemaBD.Web.Services;
using Microsoft.AspNetCore.Mvc;

namespace CinemaBD.Web.Areas.Admin.Controllers;

[AdminPermission("phanhoi", "review", "danhgia")]
public class AdminPhanHoiController : ReviewsController
{
    public AdminPhanHoiController(CinemaApiClient apiClient) : base(apiClient) { }
}
