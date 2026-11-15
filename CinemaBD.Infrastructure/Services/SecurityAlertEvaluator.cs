using CinemaBD.Infrastructure.Persistence;
using CinemaBD.Infrastructure.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CinemaBD.Infrastructure.Services;

public sealed class SecurityAlertEvaluator(AppDbContext db, SecurityAlertThresholds thresholds, ILogger<SecurityAlertEvaluator> logger)
{
    public async Task EvaluateAsync(string eventType, string? accountType, string? accountId, Guid? sessionId, string? correlationId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(accountId)) return;
        if (eventType is not ("login_failed" or "refresh_token_reuse" or "new_device_login")) return;
        var cutoff = DateTime.UtcNow.Subtract(thresholds.Window);
        var correlatedEventTypes = eventType == "login_failed"
            ? new[] { "login_failed", "mfa_login_failed" }
            : new[] { eventType };
        var count = await db.SecurityAudit.AsNoTracking()
            .Where(item => correlatedEventTypes.Contains(item.EventType) && item.AccountType == accountType && item.AccountId == accountId && item.OccurredAtUtc >= cutoff)
            .CountAsync(cancellationToken);
        var threshold = eventType switch
        {
            "login_failed" => thresholds.FailedLoginCount,
            "refresh_token_reuse" => thresholds.RefreshReuseCount,
            "new_device_login" => thresholds.NewDeviceLoginCount,
            _ => int.MaxValue
        };
        if (count < threshold || count % threshold != 0) return;
        var safeCorrelationId = SecurityMetadataSanitizer.SanitizeIdentifier(correlationId) ?? "unknown";
        logger.LogWarning("Security alert threshold reached for {EventType}, AccountType {AccountType}, AccountId {AccountId}, SessionId {SessionId}, Count {Count}, WindowSeconds {WindowSeconds}, CorrelationId {CorrelationId}.",
            eventType, accountType, accountId, sessionId, count, (int)thresholds.Window.TotalSeconds, safeCorrelationId);
    }
}
