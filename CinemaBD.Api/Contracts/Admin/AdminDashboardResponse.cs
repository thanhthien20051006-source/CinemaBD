namespace CinemaBD.Api.Contracts.Admin;

public record AdminDashboardResponse(int TotalMovies, int TotalShowtimes, int TotalCustomers, int TotalAdmins, decimal TotalPaidRevenue);
