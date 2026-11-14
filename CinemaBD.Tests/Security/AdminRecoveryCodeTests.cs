using CinemaBD.Infrastructure.Persistence;
using CinemaBD.Infrastructure.Security;
using FluentAssertions;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace CinemaBD.Tests.Security;

public sealed class AdminRecoveryCodeTests
{
    private readonly TotpMfaService _totpService;

    public AdminRecoveryCodeTests()
    {
        _totpService = new TotpMfaService(new EphemeralDataProtectionProvider());
    }

    [Fact]
    public async Task RecoveryCode_CanBeUsedOnlyOnce_SecondUseRejected()
    {
        using var db = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);

        var codes = _totpService.GenerateRecoveryCodes(10);
        codes.Should().HaveCount(10);

        int adminId = 42;
        foreach (var c in codes)
        {
            db.AdminRecoveryCodes.Add(new AdminRecoveryCode
            {
                AdminID = adminId,
                CodeHash = _totpService.HashRecoveryCode(c),
                UsedAtUtc = null
            });
        }
        await db.SaveChangesAsync();

        string codeToUse = codes[0];
        byte[] inputHash = _totpService.HashRecoveryCode(codeToUse);

        // First attempt: should find unconsumed code and mark used
        var unconsumed = await db.AdminRecoveryCodes.FirstOrDefaultAsync(
            x => x.AdminID == adminId && x.CodeHash == inputHash && x.UsedAtUtc == null);
        unconsumed.Should().NotBeNull();

        unconsumed!.UsedAtUtc = DateTime.UtcNow;
        await db.SaveChangesAsync();

        // Second attempt: code is already used, must not be found as unconsumed
        var reused = await db.AdminRecoveryCodes.FirstOrDefaultAsync(
            x => x.AdminID == adminId && x.CodeHash == inputHash && x.UsedAtUtc == null);
        reused.Should().BeNull("Used recovery code must not be reusable");
    }

    [Theory]
    [InlineData("ABCD-1234", "abcd-1234")]
    [InlineData("ABCD-1234", "ABCD1234")]
    [InlineData("ABCD-1234", "  abcd1234  ")]
    public void RecoveryCode_Normalization_MatchesRegardlessOfCaseAndDashes(string original, string input)
    {
        byte[] originalHash = _totpService.HashRecoveryCode(original);
        bool verified = _totpService.VerifyRecoveryCode(input, originalHash);

        verified.Should().BeTrue();
    }
}
