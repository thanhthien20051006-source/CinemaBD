using System.Text;
using CinemaBD.Infrastructure.Security;
using FluentAssertions;
using Microsoft.AspNetCore.DataProtection;
using Xunit;

namespace CinemaBD.Tests.Security;

public sealed class TotpMfaTests
{
    private readonly TotpMfaService _service;
    private readonly IDataProtectionProvider _dataProtectionProvider;

    public TotpMfaTests()
    {
        _dataProtectionProvider = new EphemeralDataProtectionProvider();
        _service = new TotpMfaService(_dataProtectionProvider);
    }

    [Theory]
    [InlineData(59L, "287082")]
    [InlineData(1111111109L, "081804")]
    [InlineData(1111111111L, "050471")]
    [InlineData(1234567890L, "005924")]
    [InlineData(2000000000L, "279037")]
    [InlineData(20000000000L, "353130")]
    public void ComputeTotp_MatchesRfc6238OfficialTestVectors(long timeSeconds, string expectedOtp)
    {
        // RFC 6238 Appendix B test key: ASCII "12345678901234567890" (20 bytes)
        byte[] secret = Encoding.ASCII.GetBytes("12345678901234567890");
        long timeStep = timeSeconds / 30L;

        string otp = _service.ComputeTotp(secret, timeStep);

        otp.Should().Be(expectedOtp);
    }

    [Fact]
    public void VerifyTotp_AcceptsWithinToleranceWindow_RejectsOutside()
    {
        byte[] secret = Encoding.ASCII.GetBytes("12345678901234567890");
        long currentTime = 1234567890L; // step = 41152263
        long currentStep = currentTime / 30L;

        string codeCurrent = _service.ComputeTotp(secret, currentStep);
        string codeMinusOne = _service.ComputeTotp(secret, currentStep - 1);
        string codePlusOne = _service.ComputeTotp(secret, currentStep + 1);
        string codeMinusTwo = _service.ComputeTotp(secret, currentStep - 2);
        string codePlusTwo = _service.ComputeTotp(secret, currentStep + 2);

        // Allowed window is ±1 step
        var resultCurrent = _service.VerifyTotp(secret, codeCurrent, lastUsedStep: null, unixTimeSeconds: currentTime);
        resultCurrent.IsValid.Should().BeTrue();
        resultCurrent.MatchedStep.Should().Be(currentStep);

        var resultMinusOne = _service.VerifyTotp(secret, codeMinusOne, lastUsedStep: null, unixTimeSeconds: currentTime);
        resultMinusOne.IsValid.Should().BeTrue();
        resultMinusOne.MatchedStep.Should().Be(currentStep - 1);

        var resultPlusOne = _service.VerifyTotp(secret, codePlusOne, lastUsedStep: null, unixTimeSeconds: currentTime);
        resultPlusOne.IsValid.Should().BeTrue();
        resultPlusOne.MatchedStep.Should().Be(currentStep + 1);

        // ±2 steps must be rejected
        var resultMinusTwo = _service.VerifyTotp(secret, codeMinusTwo, lastUsedStep: null, unixTimeSeconds: currentTime);
        resultMinusTwo.IsValid.Should().BeFalse();

        var resultPlusTwo = _service.VerifyTotp(secret, codePlusTwo, lastUsedStep: null, unixTimeSeconds: currentTime);
        resultPlusTwo.IsValid.Should().BeFalse();
    }

    [Fact]
    public void VerifyTotp_RejectsReplay_WhenStepLessThanOrEqualToLastUsedStep()
    {
        byte[] secret = Encoding.ASCII.GetBytes("12345678901234567890");
        long currentTime = 1234567890L;
        long currentStep = currentTime / 30L;
        string code = _service.ComputeTotp(secret, currentStep);

        // First use: lastUsedStep is prior to currentStep -> succeeds
        var firstUse = _service.VerifyTotp(secret, code, lastUsedStep: currentStep - 1, unixTimeSeconds: currentTime);
        firstUse.IsValid.Should().BeTrue();
        firstUse.IsReplay.Should().BeFalse();
        firstUse.MatchedStep.Should().Be(currentStep);

        // Replay attempt at same step: lastUsedStep == currentStep -> rejected as replay
        var replay = _service.VerifyTotp(secret, code, lastUsedStep: currentStep, unixTimeSeconds: currentTime);
        replay.IsValid.Should().BeFalse();
        replay.IsReplay.Should().BeTrue();

        // Replay attempt at older step: lastUsedStep > matchedStep -> rejected as replay
        var older = _service.VerifyTotp(secret, code, lastUsedStep: currentStep + 1, unixTimeSeconds: currentTime);
        older.IsValid.Should().BeFalse();
        older.IsReplay.Should().BeTrue();
    }

    [Fact]
    public void Base32_Roundtrip_PreservesOriginalBytes()
    {
        byte[] secret = _service.GenerateSecret(20);
        secret.Length.Should().Be(20);

        string base32 = _service.ToBase32(secret);
        base32.Should().NotBeNullOrWhiteSpace();

        byte[] restored = _service.FromBase32(base32);
        restored.Should().Equal(secret);
    }

    [Fact]
    public void GenerateOtpAuthUri_IncludesRequiredParameters()
    {
        byte[] secret = Encoding.ASCII.GetBytes("12345678901234567890");
        string base32 = _service.ToBase32(secret);

        string uri = _service.GenerateOtpAuthUri("admin_alice", base32, "CinemaBD");

        uri.Should().StartWith("otpauth://totp/CinemaBD:admin_alice?");
        uri.Should().Contain($"secret={base32}");
        uri.Should().Contain("issuer=CinemaBD");
        uri.Should().Contain("algorithm=SHA1");
        uri.Should().Contain("digits=6");
        uri.Should().Contain("period=30");
    }

    [Fact]
    public void RecoveryCodes_Generate10_AndVerifyHashes()
    {
        var codes = _service.GenerateRecoveryCodes(10);
        codes.Should().HaveCount(10);

        foreach (var code in codes)
        {
            code.Should().MatchRegex(@"^[2-9A-HJ-NP-Z]{4}-[2-9A-HJ-NP-Z]{4}$");
            byte[] hash = _service.HashRecoveryCode(code);
            hash.Length.Should().Be(32);

            _service.VerifyRecoveryCode(code, hash).Should().BeTrue();
            _service.VerifyRecoveryCode(code.ToLowerInvariant(), hash).Should().BeTrue("Case insensitive verification");
            _service.VerifyRecoveryCode(code.Replace("-", ""), hash).Should().BeTrue("Dash-insensitive verification");
            _service.VerifyRecoveryCode("WRONG-CODE", hash).Should().BeFalse();
        }
    }

    [Fact]
    public void DataProtection_EncryptAndDecryptSecret_PreservesOriginalData()
    {
        byte[] secret = _service.GenerateSecret(20);
        byte[] protectedData = _service.ProtectSecret(secret);

        protectedData.Should().NotEqual(secret, "Protected data must be encrypted");

        byte[] unprotected = _service.UnprotectSecret(protectedData);
        unprotected.Should().Equal(secret);
    }
}
