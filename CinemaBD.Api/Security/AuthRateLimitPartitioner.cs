using System.Security.Cryptography;
using System.Text;

namespace CinemaBD.Api.Security;

/// <summary>Partitions authentication request budgets by client network address or privacy-preserving account key.</summary>
public static class AuthRateLimitPartitioner
{
    public static string ByIp(HttpContext context)
        => context.Connection.RemoteIpAddress?.MapToIPv4().ToString() ?? "unknown-ip";

    public static string ByAccount(string account)
    {
        var normalized = account.Trim().ToUpperInvariant();
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(normalized));
        return Convert.ToHexString(hash.AsSpan(0, 16));
    }
}
