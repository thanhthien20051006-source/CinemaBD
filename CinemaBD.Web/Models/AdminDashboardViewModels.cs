namespace CinemaBD.Web.Models;

public class AdminDashboardApiViewModel
{
    public int TotalMovies { get; set; }
    public int TotalShowtimes { get; set; }
    public int TotalCustomers { get; set; }
    public int TotalAdmins { get; set; }
    public decimal TotalPaidRevenue { get; set; }
}
