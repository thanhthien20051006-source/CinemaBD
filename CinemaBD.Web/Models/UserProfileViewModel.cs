using System.ComponentModel.DataAnnotations;

namespace CinemaBD.Web.Models;

public class UserProfileViewModel
{
    public string UserId { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;

    [Required]
    public string FullName { get; set; } = string.Empty;

    [EmailAddress]
    public string? Email { get; set; }

    [Phone]
    public string? PhoneNumber { get; set; }

    [DataType(DataType.Date)]
    public DateTime? BirthDate { get; set; }

    public decimal TotalSpent { get; set; }
}
