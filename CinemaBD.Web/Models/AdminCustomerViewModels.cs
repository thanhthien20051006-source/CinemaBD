namespace CinemaBD.Web.Models;

public class AdminCustomerViewModel
{
    public string Id { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? PhoneNumber { get; set; }
    public DateTime? BirthDate { get; set; }
}

public class AdminCustomerPageViewModel
{
    public string? Search { get; set; }
    public IReadOnlyList<AdminCustomerViewModel> Customers { get; set; } = Array.Empty<AdminCustomerViewModel>();
}
