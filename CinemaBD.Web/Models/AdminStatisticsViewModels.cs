namespace CinemaBD.Web.Models;

public class AdminStatisticsPageViewModel
{
    public decimal TotalRevenue { get; set; }
    public int TotalOrders { get; set; }
    public int TotalTickets { get; set; }
    public int CheckedInTickets { get; set; }
    public decimal AverageOrderValue { get; set; }
    public decimal TodayRevenue { get; set; }
    public decimal CurrentMonthRevenue { get; set; }
    public decimal CurrentYearRevenue { get; set; }
    public DateTime StatisticsDate { get; set; } = DateTime.Today;
    public IReadOnlyList<AdminRevenuePointViewModel> DataPoints { get; set; } = Array.Empty<AdminRevenuePointViewModel>();
    public IReadOnlyList<AdminPaymentMethodStatViewModel> PaymentMethods { get; set; } = Array.Empty<AdminPaymentMethodStatViewModel>();
    public IReadOnlyList<AdminRevenuePointViewModel> TopMovies { get; set; } = Array.Empty<AdminRevenuePointViewModel>();
    public IReadOnlyList<AdminRevenuePointViewModel> DailyData { get; set; } = Array.Empty<AdminRevenuePointViewModel>();
    public IReadOnlyList<AdminRevenuePointViewModel> HourlyData { get; set; } = Array.Empty<AdminRevenuePointViewModel>();
    public IReadOnlyList<AdminRevenuePointViewModel> WeekdayData { get; set; } = Array.Empty<AdminRevenuePointViewModel>();
    public IReadOnlyList<AdminRevenuePointViewModel> TopCombos { get; set; } = Array.Empty<AdminRevenuePointViewModel>();
    public IReadOnlyList<AdminRevenuePointViewModel> TopCustomers { get; set; } = Array.Empty<AdminRevenuePointViewModel>();
}

public class AdminRevenuePointViewModel
{
    public string Label { get; set; } = string.Empty;
    public decimal Value { get; set; }
}

public class AdminPaymentMethodStatViewModel
{
    public string Method { get; set; } = string.Empty;
    public int OrderCount { get; set; }
    public decimal Revenue { get; set; }
}
