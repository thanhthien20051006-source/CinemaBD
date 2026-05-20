using CinemaBD.Application.Interfaces;
using CinemaBD.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace CinemaBD.Infrastructure.Services;

/// <summary>
/// Background service tự động đánh dấu "Expired" cho các suất chiếu đã qua giờ.
/// Chạy mỗi 1 phút.
/// </summary>
public class ShowtimeExpiryHostedService : BackgroundService
{
    private static readonly TimeSpan Interval = TimeSpan.FromMinutes(1);

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<ShowtimeExpiryHostedService> _logger;

    public ShowtimeExpiryHostedService(
        IServiceScopeFactory scopeFactory,
        ILogger<ShowtimeExpiryHostedService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("[ShowtimeExpiry] Background service khởi động.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await using var scope = _scopeFactory.CreateAsyncScope();
                var service = scope.ServiceProvider.GetRequiredService<IAdminShowtimeService>();
                var expired = await service.ExpirePassedShowtimesAsync(stoppingToken);

                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                var holdExpiredBefore = DateTime.Now.AddMinutes(-10);
                var expiredTickets = await db.Tickets
                    .Where(v => v.TrangThai == "Pending" && v.NgayDat <= holdExpiredBefore)
                    .ToListAsync(stoppingToken);

                if (expiredTickets.Count > 0)
                {
                    var txnRefs = expiredTickets
                        .Select(v => v.GatewayTxnRef)
                        .Where(x => !string.IsNullOrWhiteSpace(x))
                        .Distinct()
                        .ToList();

                    foreach (var ticket in expiredTickets)
                        ticket.TrangThai = "Expired";

                    var payments = await db.Payments
                        .Where(p => p.TrangThai == "Pending" && p.GatewayTxnRef != null && txnRefs.Contains(p.GatewayTxnRef))
                        .ToListAsync(stoppingToken);

                    foreach (var payment in payments)
                        payment.TrangThai = "Expired";

                    await db.SaveChangesAsync(stoppingToken);
                }

                if (expired > 0 || expiredTickets.Count > 0)
                    _logger.LogInformation("[ShowtimeExpiry] Đã expire {ShowtimeCount} suất chiếu, {TicketCount} vé pending.", expired, expiredTickets.Count);
            }
            catch (Exception ex) when (!stoppingToken.IsCancellationRequested)
            {
                _logger.LogError(ex, "[ShowtimeExpiry] Lỗi khi expire suất chiếu.");
            }

            await Task.Delay(Interval, stoppingToken);
        }
    }
}




