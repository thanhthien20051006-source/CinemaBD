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
        var connectionString = configuration.GetConnectionString("DefaultConnection");
        if (string.IsNullOrWhiteSpace(connectionString))
            throw new InvalidOperationException("Missing required configuration ConnectionStrings__DefaultConnection.");

        var connectionBuilder = new Microsoft.Data.SqlClient.SqlConnectionStringBuilder(connectionString);
        if (string.IsNullOrWhiteSpace(connectionBuilder.InitialCatalog))
            throw new InvalidOperationException("ConnectionStrings__DefaultConnection must include Database/Initial Catalog.");

        services.AddDbContext<AppDbContext>(options => options.UseSqlServer(connectionString));
        if (configuration.GetValue<bool>("ShowtimeExpiry:Enabled", false))
            services.AddHostedService<ShowtimeExpiryHostedService>();

        services.AddScoped<Pbkdf2PasswordHasher>();
        services.AddScoped<IAccountPasswordService, AccountPasswordService>();
        services.AddSingleton<CinemaBD.Infrastructure.Security.SecurityAlertThresholds>();
        services.AddScoped<SecurityAlertEvaluator>();
        services.AddScoped<ISecurityAuditWriter, SecurityAuditWriter>();
        services.AddScoped<AccountLockoutService>();
        services.AddScoped<IAccountLockoutService>(provider => provider.GetRequiredService<AccountLockoutService>());
        services.AddScoped<IAdministrativeAccountLockoutService>(provider => provider.GetRequiredService<AccountLockoutService>());
        services.AddScoped<VnPayUrlBuilder>();
        services.AddScoped<VnPaySignatureValidator>();
        services.AddScoped<DatabaseInitializer>();
        services.AddScoped<ITokenService, JwtTokenService>();
        services.AddScoped<ITotpMfaService, TotpMfaService>();
        services.AddScoped<IAdminEmergencyRecoveryService, AdminEmergencyRecoveryService>();
        services.AddScoped<IRevokedTokenService, RevokedTokenService>();
        services.AddScoped<IIdentitySessionService, IdentitySessionService>();
        services.AddScoped<IAdminSecurityResponseService, AdminSecurityResponseService>();
        services.AddScoped<ISecurityAuditQueryService, SecurityAuditQueryService>();
        services.AddScoped<ISecurityDataCleanupService, SecurityDataCleanupService>();
        if (configuration.GetValue("Security:Cleanup:Enabled", true))
            services.AddHostedService<SecurityDataCleanupHostedService>();
        services.AddScoped<IRefreshTokenStore, EfRefreshTokenStore>();
        services.AddScoped<IRefreshTokenService, RefreshTokenService>();
        services.AddSingleton<IQrCodeSecurityService, HmacQrCodeSecurityService>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IAdminAuthApiService, AdminAuthApiService>();
        services.AddScoped<IAdminDashboardService, AdminDashboardService>();
        services.AddScoped<IAdminMovieService, AdminMovieService>();
        services.AddScoped<IAdminShowtimeService, AdminShowtimeService>();
        services.AddScoped<IAdminCustomerService, AdminCustomerService>();
        services.AddScoped<IAdminEmployeeService, AdminEmployeeService>();
        services.AddScoped<IAdminRoleService, AdminRoleService>();
        services.AddScoped<IAdminAuthorizationService, AdminAuthorizationService>();
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
        services.AddScoped<IVoucherPreviewService, VoucherPreviewService>();
        services.AddScoped<IAdminLoyaltyPointService, AdminLoyaltyPointService>();
        services.AddScoped<IAdminRefundService, AdminRefundService>();
        services.AddScoped<ILoyaltyPointService, LoyaltyPointService>();
        services.AddScoped<IArticleService, ArticleService>();
        services.AddScoped<IEventService, EventService>();
        services.AddScoped<IMovieService, MovieService>();
        services.AddScoped<IShowtimeService, ShowtimeService>();
        services.AddScoped<ISeatService, SeatService>();
        services.AddScoped<IBookingService, BookingService>();
        services.AddScoped<ICustomerProfileService, CustomerProfileService>();
        services.AddScoped<IPaymentService, PaymentService>();
        services.AddScoped<IReviewService, ReviewService>();
        services.AddScoped<IAdminReviewService, AdminReviewService>();

        return services;
    }
}


