using CinemaBD.Application.Interfaces;
using CinemaBD.Infrastructure.Payments;
using CinemaBD.Infrastructure.Services;
using CinemaBD.Infrastructure.Persistence;
using CinemaBD.Infrastructure.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CinemaBD.Infrastructure.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<AppDbContext>(options =>
            options.UseSqlServer(configuration.GetConnectionString("DefaultConnection")));

        services.AddScoped<Md5PasswordHasher>();
        services.AddScoped<VnPayUrlBuilder>();
        services.AddScoped<VnPaySignatureValidator>();
        services.AddScoped<DatabaseInitializer>();
        services.AddScoped<ITokenService, JwtTokenService>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IAdminAuthApiService, AdminAuthApiService>();
        services.AddScoped<IAdminDashboardService, AdminDashboardService>();
        services.AddScoped<IAdminMovieService, AdminMovieService>();
        services.AddScoped<IAdminShowtimeService, AdminShowtimeService>();
        services.AddScoped<IAdminCustomerService, AdminCustomerService>();
        services.AddScoped<IAdminEmployeeService, AdminEmployeeService>();
        services.AddScoped<IAdminRoleService, AdminRoleService>();
        services.AddScoped<IAdminCinemaService, AdminCinemaService>();
        services.AddScoped<IAdminRoomService, AdminRoomService>();
        services.AddScoped<IAdminSeatService, AdminSeatService>();
        services.AddScoped<IAdminGenreService, AdminGenreService>();
        services.AddScoped<IAdminArticleService, AdminArticleService>();
        services.AddScoped<IAdminEventService, AdminEventService>();
        services.AddScoped<IAdminComboService, AdminComboService>();
        services.AddScoped<IAdminInvoiceService, AdminInvoiceService>();
        services.AddScoped<IAdminStatisticsService, AdminStatisticsService>();
        services.AddScoped<IAdminVoucherService, AdminVoucherService>();
        services.AddScoped<IAdminLoyaltyPointService, AdminLoyaltyPointService>();
        services.AddScoped<IAdminRefundService, AdminRefundService>();
        services.AddScoped<ILoyaltyPointService, LoyaltyPointService>();
        services.AddScoped<IMovieService, MovieService>();
        services.AddScoped<IShowtimeService, ShowtimeService>();
        services.AddScoped<ISeatService, SeatService>();
        services.AddScoped<IBookingService, BookingService>();
        services.AddScoped<ICustomerProfileService, CustomerProfileService>();
        services.AddScoped<IPaymentService, PaymentService>();
        services.AddScoped<IReviewService, ReviewService>();
        services.AddScoped<IAdminReviewService, AdminReviewService>();

        // Background service: tự động expire suất chiếu đã qua giờ
        services.AddHostedService<ShowtimeExpiryHostedService>();

        return services;
    }
}


