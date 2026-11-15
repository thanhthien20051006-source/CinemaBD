using CinemaBD.Application.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace CinemaBD.Infrastructure.Services;

public sealed class SecurityDataCleanupHostedService(IServiceScopeFactory scopeFactory, IConfiguration configuration, ILogger<SecurityDataCleanupHostedService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var intervalMinutes = Math.Clamp(configuration.GetValue("Security:Cleanup:IntervalMinutes", 60), 1, 1440);
        var auditRetentionDays = Math.Clamp(configuration.GetValue("Security:Audit:RetentionDays", 365), 1, 3650);
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(TimeSpan.FromMinutes(intervalMinutes), stoppingToken);
                if (!configuration.GetValue("Security:Cleanup:Enabled", true)
                    || configuration.GetValue("Security:Cleanup:LegalHold", false))
                    continue;

                await using var scope = scopeFactory.CreateAsyncScope();
                var cleanup = scope.ServiceProvider.GetRequiredService<ISecurityDataCleanupService>();
                var result = await cleanup.CleanupAsync(DateTime.UtcNow, TimeSpan.FromDays(auditRetentionDays), stoppingToken);
                logger.LogInformation("Security retention cleanup completed. AuditDeleted {AuditDeleted}, SessionsDeleted {SessionsDeleted}, RefreshTokensDeleted {RefreshTokensDeleted}, RevokedTokensDeleted {RevokedTokensDeleted}.",
                    result.AuditRecordsDeleted, result.SessionsDeleted, result.RefreshTokensDeleted, result.RevokedTokensDeleted);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Security retention cleanup failed; investigation data was not intentionally purged beyond configured retention.");
            }
        }
    }
}
