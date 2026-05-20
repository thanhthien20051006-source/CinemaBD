namespace CinemaBD.Domain.Entities;

public class RevenueStatistics
{
    public decimal TotalRevenue { get; set; }
    public int TotalOrders { get; set; }
    public int TotalTickets { get; set; }
    public int CheckedInTickets { get; set; }
    public decimal AverageOrderValue { get; set; }
    public decimal TodayRevenue { get; set; }
    public decimal CurrentMonthRevenue { get; set; }
    public decimal CurrentYearRevenue { get; set; }
    public DateTime StatisticsDate { get; set; }
    public List<RevenueDataPoint> MonthlyData { get; set; } = new();
    public List<PaymentMethodStat> PaymentMethods { get; set; } = new();
    public List<RevenueDataPoint> TopMovies { get; set; } = new();
    public List<RevenueDataPoint> DailyData { get; set; } = new();
    public List<RevenueDataPoint> HourlyData { get; set; } = new();
    public List<RevenueDataPoint> WeekdayData { get; set; } = new();
    public List<RevenueDataPoint> TopCombos { get; set; } = new();
    public List<RevenueDataPoint> TopCustomers { get; set; } = new();
}

public class RevenueDataPoint
{
    public string Label { get; set; } = default!;
    public decimal Value { get; set; }
}

public class PaymentMethodStat
{
    public string Method { get; set; } = default!;
    public int OrderCount { get; set; }
    public decimal Revenue { get; set; }
}
