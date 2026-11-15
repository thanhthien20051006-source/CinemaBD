using Microsoft.Extensions.Configuration;

namespace CinemaBD.Infrastructure.Security;

public sealed class SecurityAlertThresholds
{
    public int FailedLoginCount { get; }
    public int RefreshReuseCount { get; }
    public int NewDeviceLoginCount { get; }
    public TimeSpan Window { get; }

    public SecurityAlertThresholds(IConfiguration configuration)
    {
        FailedLoginCount = Math.Clamp(configuration.GetValue("Security:Alerts:FailedLoginCount", 5), 1, 10000);
        RefreshReuseCount = Math.Clamp(configuration.GetValue("Security:Alerts:RefreshReuseCount", 1), 1, 10000);
        NewDeviceLoginCount = Math.Clamp(configuration.GetValue("Security:Alerts:NewDeviceLoginCount", 3), 1, 10000);
        Window = TimeSpan.FromMinutes(Math.Clamp(configuration.GetValue("Security:Alerts:WindowMinutes", 10), 1, 1440));
    }
}
