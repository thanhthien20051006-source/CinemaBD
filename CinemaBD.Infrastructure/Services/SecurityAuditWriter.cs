using CinemaBD.Application.Interfaces;
using CinemaBD.Infrastructure.Persistence;
using CinemaBD.Infrastructure.Security;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace CinemaBD.Infrastructure.Services;

/// <summary>Single persistence boundary for sanitized security audit events.</summary>
public sealed class SecurityAuditWriter(AppDbContext db, IHttpContextAccessor? httpContextAccessor = null, SecurityAlertEvaluator? alertEvaluator = null, ILogger<SecurityAuditWriter>? logger = null) : ISecurityAuditWriter
{
    public async Task WriteAsync(SecurityAuditEvent auditEvent, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(auditEvent);

        var eventType = SanitizeEventType(auditEvent.EventType);
        var claims = httpContextAccessor?.HttpContext?.User;
        var actorType = auditEvent.ActorType ?? claims?.FindFirst("account_type")?.Value;
        var actorId = auditEvent.ActorId
            ?? claims?.FindFirst(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub)?.Value
            ?? claims?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        var safeActorType = SanitizeField(actorType, 32, "unknown");
        var safeActorId = SecurityMetadataSanitizer.SanitizeIdentifier(actorId);
        var sessionId = auditEvent.SessionId
            ?? (Guid.TryParse(claims?.FindFirst("sid")?.Value, out var parsedSession) ? parsedSession : null);

        var request = httpContextAccessor?.HttpContext?.Request;
        var sourceIp = auditEvent.SourceIp ?? request?.HttpContext.Connection.RemoteIpAddress?.ToString();
        var safeSourceIp = SecurityMetadataSanitizer.SanitizeIp(sourceIp);
        var deviceName = auditEvent.DeviceName ?? request?.Headers["X-Device-Name"].ToString();
        var userAgent = auditEvent.UserAgent ?? request?.Headers["User-Agent"].ToString();
        var safeDevice = SecurityMetadataSanitizer.SanitizeDeviceName(deviceName);
        var safeUserAgent = SecurityMetadataSanitizer.SanitizeUserAgent(userAgent);
        var safeResult = SanitizeField(auditEvent.Result, 32, "unknown") ?? "unknown";
        var safeCorrelationId = SecurityMetadataSanitizer.SanitizeIdentifier(
            auditEvent.CorrelationId ?? httpContextAccessor?.HttpContext?.TraceIdentifier);
        string? safeDetails;
        try
        {
            safeDetails = SecurityMetadataSanitizer.SanitizeDetails(auditEvent.Details);
        }
        catch (ArgumentException)
        {
            safeDetails = "Details omitted by security sanitizer.";
        }
        var metadata = BuildMetadata(safeDevice, safeUserAgent, safeResult, safeDetails);
        var actorAndDevice = safeActorType;
        db.SecurityAudit.Add(new SecurityAuditRecord
        {
            OccurredAtUtc = DateTime.UtcNow,
            EventType = eventType,
            AccountType = actorAndDevice,
            AccountId = safeActorId,
            SessionId = sessionId,
            SourceIp = safeSourceIp,
            CorrelationId = safeCorrelationId,
            Details = metadata
        });
        await db.SaveChangesAsync(cancellationToken);
        if (alertEvaluator is not null)
        {
            try
            {
                await alertEvaluator.EvaluateAsync(eventType, safeActorType, safeActorId, sessionId, safeCorrelationId, cancellationToken);
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                logger?.LogWarning(exception, "Security alert evaluation failed for {EventType}, CorrelationId {CorrelationId}.", eventType, safeCorrelationId);
            }
        }
    }

    private static string BuildMetadata(string? deviceName, string? userAgent, string result, string? details)
    {
        var original = System.Text.Json.JsonSerializer.Serialize(new AuditMetadata(deviceName, userAgent, result, details));
        try
        {
            SecurityMetadataSanitizer.SanitizeAuditContext(original);
        }
        catch (ArgumentException)
        {
            // Discard the complete untrusted context if any field resembles credential material.
            return System.Text.Json.JsonSerializer.Serialize(new AuditMetadata(null, null, "unknown", "Sensitive metadata omitted by security sanitizer."));
        }
        return original.Length <= 1000 ? original : original[..1000];
    }

    private static string SanitizeEventType(string value)
    {
        var safe = SecurityMetadataSanitizer.SanitizeIdentifier(value);
        if (string.IsNullOrWhiteSpace(safe) || safe.Length > 64
            || safe.Any(character => !char.IsAsciiLetterOrDigit(character) && character != '_' && character != '-'))
            throw new ArgumentException("Security audit event type is invalid.", nameof(value));
        return safe;
    }

    private static string? SanitizeField(string? value, int maxLength, string? fallback)
    {
        if (string.IsNullOrWhiteSpace(value)) return fallback;
        var safe = SecurityMetadataSanitizer.SanitizeIdentifier(value.Trim());
        if (string.IsNullOrWhiteSpace(safe)) return fallback;
        if (safe.Length > maxLength) safe = safe[..maxLength];
        return safe.All(character => char.IsAsciiLetterOrDigit(character) || character is '_' or '-' or '.') ? safe : fallback;
    }

    private static string? SanitizeIp(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        value = value.Trim();
        return value.Length <= 45 && System.Net.IPAddress.TryParse(value, out _) ? value : null;
    }

    private sealed record AuditMetadata(string? DeviceName, string? UserAgent, string Result, string? Details);
}
