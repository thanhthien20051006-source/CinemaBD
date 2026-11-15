using CinemaBD.Infrastructure.Configuration;
using FluentAssertions;
using Microsoft.Extensions.Configuration;

namespace CinemaBD.Tests.Configuration;

public class DotEnvLoaderTests : IDisposable
{
    private readonly string _tempDir;

    public DotEnvLoaderTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "DotEnvLoaderTests_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempDir);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
        {
            try { Directory.Delete(_tempDir, true); } catch { }
        }
    }

    [Fact]
    public void Load_WhenDotEnvExists_ShouldPopulateMissingSettings()
    {
        var envPath = Path.Combine(_tempDir, ".env");
        File.WriteAllLines(envPath, new[]
        {
            "# Test comment",
            "LOCAL_SQL_CONNECTION=Server=host.docker.internal,1433;Database=CinemaBD;User Id=sa;Password=Secret123!;TrustServerCertificate=True",
            "JWT_KEY=SuperSecretJwtKeyForTestingAtLeast32BytesLong!",
            "JWT_ISSUER=CinemaBD.Test",
            "JWT_AUDIENCE=CinemaBD.Client.Test",
            "QR_SIGNING_KEY=SuperSecretQrKeyForTestingAtLeast32BytesLong!"
        });

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Key"] = "CHANGE_ME_TO_A_LONG_RANDOM_SECRET",
                ["Security:QrSigningKey"] = "CHANGE_ME_TO_A_RANDOM_SECRET_AT_LEAST_32_BYTES"
            })
            .Build();

        DotEnvLoader.Load(configuration, _tempDir);

        configuration.GetConnectionString("DefaultConnection").Should().Contain("localhost,1433");
        configuration.GetConnectionString("DefaultConnection").Should().NotContain("host.docker.internal");
        configuration["Jwt:Key"].Should().Be("SuperSecretJwtKeyForTestingAtLeast32BytesLong!");
        configuration["Jwt:Issuer"].Should().Be("CinemaBD.Test");
        configuration["Jwt:Audience"].Should().Be("CinemaBD.Client.Test");
        configuration["Security:QrSigningKey"].Should().Be("SuperSecretQrKeyForTestingAtLeast32BytesLong!");
    }

    [Fact]
    public void Load_WhenConfigurationAlreadyHasRealValue_ShouldNotOverwrite()
    {
        var envPath = Path.Combine(_tempDir, ".env");
        File.WriteAllLines(envPath, new[]
        {
            "LOCAL_SQL_CONNECTION=Server=env_server;Database=EnvDb;User Id=sa;Password=pwd;",
            "JWT_KEY=KeyFromEnvFileThatIsAtLeast32BytesLong!",
            "DATABASE_ALLOW_AUTO_MIGRATE=false"
        });

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = "Server=existing_server;Database=ExistingDb;User Id=sa;Password=pwd;",
                ["Jwt:Key"] = "ExistingKeyThatIsAlreadyConfiguredAtLeast32Bytes!",
                ["Database:AllowAutoMigrate"] = "true"
            })
            .Build();

        DotEnvLoader.Load(configuration, _tempDir);

        configuration.GetConnectionString("DefaultConnection").Should().Contain("existing_server");
        configuration["Jwt:Key"].Should().Be("ExistingKeyThatIsAlreadyConfiguredAtLeast32Bytes!");
        configuration["Database:AllowAutoMigrate"].Should().Be("true");
    }

    [Fact]
    public void Load_WhenNoDotEnvFileExists_ShouldNotThrow()
    {
        var emptyDir = Path.Combine(_tempDir, "empty");
        Directory.CreateDirectory(emptyDir);
        var configuration = new ConfigurationBuilder().Build();

        var act = () => DotEnvLoader.Load(configuration, emptyDir);

        act.Should().NotThrow();
    }
}
