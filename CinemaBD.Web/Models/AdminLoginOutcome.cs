using CinemaBD.Web.Services;

namespace CinemaBD.Web.Models;

public sealed record AdminLoginOutcome(CinemaApiClient.AdminAuthResult? Admin, string? ErrorMessage = null, bool IsLocked = false);
