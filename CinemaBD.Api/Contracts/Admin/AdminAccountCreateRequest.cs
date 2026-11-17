using System.ComponentModel.DataAnnotations;

namespace CinemaBD.Api.Contracts.Admin;

public sealed class AdminAccountCreateRequest
{
    [Required]
    [StringLength(32, MinimumLength = 3)]
    [RegularExpression("^[A-Za-z0-9_]{3,32}$")]
    public string Username { get; init; } = string.Empty;

    [Required]
    [StringLength(100, MinimumLength = 1)]
    public string FullName { get; init; } = string.Empty;

    [Range(1, int.MaxValue)]
    public int RoleId { get; init; }
}
