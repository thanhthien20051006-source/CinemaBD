using System.Security.Cryptography;
using System.Text;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Configuration;

namespace CinemaBD.Api.Security;

/// <summary>Owns independent IP and privacy-preserving account partitions for auth endpoints.</summary>
public sealed class AuthAccountRateLimiter : IDisposable
{
    private readonly PartitionedRateLimiter<HttpContext> _ipLimiter;
    private readonly PartitionedRateLimiter<string> _accountLimiter;
    private readonly int _ipPermitLimit;
    private readonly int _accountPermitLimit;
    private readonly TimeSpan _window;

    public AuthAccountRateLimiter(IConfiguration configuration)
    {
        _ipPermitLimit = Math.Clamp(configuration.GetValue("Security:RateLimits:AuthIpPermitLimit", 10), 1, 1000);
        _accountPermitLimit = Math.Clamp(configuration.GetValue("Security:RateLimits:AuthAccountPermitLimit", 5), 1, 1000);
        _window = TimeSpan.FromSeconds(Math.Clamp(configuration.GetValue("Security:RateLimits:AuthWindowSeconds", 60), 1, 3600));
        _ipLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context => RateLimitPartition.GetFixedWindowLimiter(
            AuthRateLimitPartitioner.ByIp(context),
            _ => new FixedWindowRateLimiterOptions { PermitLimit = _ipPermitLimit, Window = _window, QueueLimit = 0, AutoReplenishment = true }));
        _accountLimiter = PartitionedRateLimiter.Create<string, string>(account => RateLimitPartition.GetFixedWindowLimiter(
            HashAccount(account),
            _ => new FixedWindowRateLimiterOptions { PermitLimit = _accountPermitLimit, Window = _window, QueueLimit = 0, AutoReplenishment = true }));
    }

    public AuthAccountRateLimiter(int accountPermitLimit, TimeSpan window)
    {
        _ipPermitLimit = 1000;
        _accountPermitLimit = Math.Clamp(accountPermitLimit, 1, 1000);
        _window = window;
        _ipLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context => RateLimitPartition.GetFixedWindowLimiter(
            AuthRateLimitPartitioner.ByIp(context),
            _ => new FixedWindowRateLimiterOptions { PermitLimit = _ipPermitLimit, Window = _window, QueueLimit = 0, AutoReplenishment = true }));
        _accountLimiter = PartitionedRateLimiter.Create<string, string>(account => RateLimitPartition.GetFixedWindowLimiter(
            HashAccount(account),
            _ => new FixedWindowRateLimiterOptions { PermitLimit = _accountPermitLimit, Window = _window, QueueLimit = 0, AutoReplenishment = true }));
    }

    public RateLimitLease Acquire(HttpContext context, string account)
    {
        var ipLease = _ipLimiter.AttemptAcquire(context);
        if (!ipLease.IsAcquired) return ipLease;
        var accountLease = _accountLimiter.AttemptAcquire(account);
        if (!accountLease.IsAcquired)
        {
            ipLease.Dispose();
            return accountLease;
        }
        return new CombinedLease(ipLease, accountLease);
    }

    private sealed class CombinedLease(RateLimitLease ipLease, RateLimitLease accountLease) : RateLimitLease
    {
        public override bool IsAcquired => ipLease.IsAcquired && accountLease.IsAcquired;
        public override IEnumerable<string> MetadataNames => !ipLease.IsAcquired ? ipLease.MetadataNames : accountLease.MetadataNames;
        public override bool TryGetMetadata(string metadataName, out object? metadata)
            => (!ipLease.IsAcquired ? ipLease : accountLease).TryGetMetadata(metadataName, out metadata);

        protected override void Dispose(bool disposing)
        {
            if (!disposing) return;
            accountLease.Dispose();
            ipLease.Dispose();
        }
    }

    public void Dispose()
    {
        _ipLimiter.Dispose();
        _accountLimiter.Dispose();
    }

    private static string HashAccount(string account)
    {
        var normalized = account.Trim().ToUpperInvariant();
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(normalized));
        return Convert.ToHexString(hash.AsSpan(0, 16));
    }
}
