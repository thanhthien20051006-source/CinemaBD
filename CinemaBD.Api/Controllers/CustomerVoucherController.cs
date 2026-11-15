using CinemaBD.Api.Contracts.Booking;
using CinemaBD.Api.Contracts.Common;
using CinemaBD.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace CinemaBD.Api.Controllers;

[ApiController]
[Authorize(Policy = "NotPasswordChangeOnly")]
[Route("api/account/vouchers")]
public sealed class CustomerVoucherController : ControllerBase
{
    private readonly IVoucherPreviewService _voucherPreviewService;

    public CustomerVoucherController(IVoucherPreviewService voucherPreviewService)
    {
        _voucherPreviewService = voucherPreviewService;
    }

}
