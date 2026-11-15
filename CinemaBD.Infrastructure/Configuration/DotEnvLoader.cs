using Microsoft.Extensions.Configuration;

namespace CinemaBD.Infrastructure.Configuration;

public static class DotEnvLoader
{
    public static void Load(IConfiguration configuration, string? startPath = null)
    {
        var envFilePath = FindDotEnvFile(startPath);
        if (envFilePath == null || !File.Exists(envFilePath))
            return;

        var envVars = ParseDotEnvFile(envFilePath);
        if (envVars.Count == 0)
            return;

        // 1. Populate process-level environment variables if not already set
        foreach (var (key, value) in envVars)
        {
            if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable(key)))
            {
                Environment.SetEnvironmentVariable(key, value);
            }
        }

        // 2. Resolve database connection string
        var isOutsideContainer = !File.Exists("/.dockerenv") &&
                                 Environment.GetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER") != "true";

        if (string.IsNullOrWhiteSpace(configuration.GetConnectionString("DefaultConnection")))
        {
            string? connStr = null;
            if (envVars.TryGetValue("ConnectionStrings__DefaultConnection", out var explicitConn) && !string.IsNullOrWhiteSpace(explicitConn))
            {
                connStr = explicitConn;
            }
            else if (envVars.TryGetValue("LOCAL_SQL_CONNECTION", out var localConn) && !string.IsNullOrWhiteSpace(localConn))
            {
                connStr = localConn;
            }

            if (!string.IsNullOrWhiteSpace(connStr))
            {
                // When running on the host outside Docker, host.docker.internal is not resolvable by default.
                // Replace with localhost to connect to the host's SQL Server.
                if (isOutsideContainer && connStr.Contains("host.docker.internal", StringComparison.OrdinalIgnoreCase))
                {
                    connStr = connStr.Replace("host.docker.internal", "localhost", StringComparison.OrdinalIgnoreCase);
                }

                configuration["ConnectionStrings:DefaultConnection"] = connStr;
                Environment.SetEnvironmentVariable("ConnectionStrings__DefaultConnection", connStr);
            }
        }

        // 3. Map JWT settings
        if (IsPlaceholderOrEmpty(configuration["Jwt:Key"]) && envVars.TryGetValue("JWT_KEY", out var jwtKey))
        {
            configuration["Jwt:Key"] = jwtKey;
            Environment.SetEnvironmentVariable("Jwt__Key", jwtKey);
        }
        if (string.IsNullOrWhiteSpace(configuration["Jwt:Issuer"]) && envVars.TryGetValue("JWT_ISSUER", out var jwtIssuer))
        {
            configuration["Jwt:Issuer"] = jwtIssuer;
            Environment.SetEnvironmentVariable("Jwt__Issuer", jwtIssuer);
        }
        if (string.IsNullOrWhiteSpace(configuration["Jwt:Audience"]) && envVars.TryGetValue("JWT_AUDIENCE", out var jwtAudience))
        {
            configuration["Jwt:Audience"] = jwtAudience;
            Environment.SetEnvironmentVariable("Jwt__Audience", jwtAudience);
        }

        // 4. Map QR signing key
        if (IsPlaceholderOrEmpty(configuration["Security:QrSigningKey"]) && envVars.TryGetValue("QR_SIGNING_KEY", out var qrKey))
        {
            configuration["Security:QrSigningKey"] = qrKey;
            Environment.SetEnvironmentVariable("Security__QrSigningKey", qrKey);
        }

        // 5. Map Database initializer guard flags
        if (string.IsNullOrWhiteSpace(configuration["Database:AllowAutoMigrate"]) && envVars.TryGetValue("DATABASE_ALLOW_AUTO_MIGRATE", out var allowMigrate))
        {
            configuration["Database:AllowAutoMigrate"] = allowMigrate;
        }
        if (string.IsNullOrWhiteSpace(configuration["Database:AllowedNames:0"]) && envVars.TryGetValue("DATABASE_ALLOWED_NAME", out var allowedName))
        {
            configuration["Database:AllowedNames:0"] = allowedName;
        }
        if (string.IsNullOrWhiteSpace(configuration["DatabaseSeed:AllowDemoData"]) && envVars.TryGetValue("DATABASE_ALLOW_DEMO_DATA", out var allowDemo))
        {
            configuration["DatabaseSeed:AllowDemoData"] = allowDemo;
        }
        if (string.IsNullOrWhiteSpace(configuration["ShowtimeExpiry:Enabled"]) && envVars.TryGetValue("SHOWTIME_EXPIRY_ENABLED", out var showtimeExpiry))
        {
            configuration["ShowtimeExpiry:Enabled"] = showtimeExpiry;
        }

        // 6. Map CORS & VNPAY & other common settings
        if (string.IsNullOrWhiteSpace(configuration["Cors:AllowedOrigins:0"]) && envVars.TryGetValue("CORS_ALLOWED_ORIGIN", out var corsOrigin))
        {
            configuration["Cors:AllowedOrigins:0"] = corsOrigin;
        }
        if (string.IsNullOrWhiteSpace(configuration["VnPay:BaseUrl"]) && envVars.TryGetValue("VNPAY_BASE_URL", out var vnpBase))
        {
            configuration["VnPay:BaseUrl"] = vnpBase;
        }
        if (IsPlaceholderOrEmpty(configuration["VnPay:TmnCode"]) && envVars.TryGetValue("VNPAY_TMN_CODE", out var vnpTmn))
        {
            configuration["VnPay:TmnCode"] = vnpTmn;
        }
        if (IsPlaceholderOrEmpty(configuration["VnPay:HashSecret"]) && envVars.TryGetValue("VNPAY_HASH_SECRET", out var vnpSecret))
        {
            configuration["VnPay:HashSecret"] = vnpSecret;
        }
        if (envVars.TryGetValue("WEB_PUBLIC_URL", out var webPublicUrl) && !string.IsNullOrWhiteSpace(webPublicUrl))
        {
            if (string.IsNullOrWhiteSpace(configuration["VnPay:ReturnUrl"]))
            {
                configuration["VnPay:ReturnUrl"] = $"{webPublicUrl.TrimEnd('/')}/booking/payment-return";
            }
        }

        // 7. Generic fallback for any other keys in .env
        foreach (var (key, value) in envVars)
        {
            if (key.Contains("__"))
            {
                var configKey = key.Replace("__", ":");
                if (IsPlaceholderOrEmpty(configuration[configKey]))
                {
                    configuration[configKey] = value;
                }
            }
            else if (IsPlaceholderOrEmpty(configuration[key]))
            {
                configuration[key] = value;
            }
        }
    }

    private static bool IsPlaceholderOrEmpty(string? value) =>
        string.IsNullOrWhiteSpace(value) || value.StartsWith("CHANGE_ME", StringComparison.OrdinalIgnoreCase);

    private static string? FindDotEnvFile(string? startPath)
    {
        var searchDir = string.IsNullOrWhiteSpace(startPath) ? Directory.GetCurrentDirectory() : startPath;
        if (File.Exists(searchDir))
        {
            searchDir = Path.GetDirectoryName(searchDir);
        }
        if (string.IsNullOrWhiteSpace(searchDir))
        {
            searchDir = Directory.GetCurrentDirectory();
        }

        var dirInfo = new DirectoryInfo(searchDir);

        while (dirInfo != null && dirInfo.Exists)
        {
            var candidate = Path.Combine(dirInfo.FullName, ".env");
            if (File.Exists(candidate))
                return candidate;

            dirInfo = dirInfo.Parent;
        }

        var cwdCandidate = Path.Combine(Directory.GetCurrentDirectory(), ".env");
        if (File.Exists(cwdCandidate))
            return cwdCandidate;

        return null;
    }

    private static Dictionary<string, string> ParseDotEnvFile(string filePath)
    {
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var rawLine in File.ReadAllLines(filePath))
        {
            var line = rawLine.Trim();
            if (string.IsNullOrEmpty(line) || line.StartsWith('#'))
                continue;

            var eqIndex = line.IndexOf('=');
            if (eqIndex <= 0)
                continue;

            var key = line.Substring(0, eqIndex).Trim();
            var val = line.Substring(eqIndex + 1).Trim();

            if (val.Length >= 2 &&
                ((val.StartsWith('"') && val.EndsWith('"')) ||
                 (val.StartsWith('\'') && val.EndsWith('\''))))
            {
                val = val.Substring(1, val.Length - 2);
            }

            result[key] = val;
        }
        return result;
    }
}
