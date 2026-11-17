using System.Text;
using CinemaBD.Web.Core;
using FluentAssertions;

namespace CinemaBD.Tests.Web;

public sealed class AdminSessionGateTests
{
    [Fact]
    public void IsAllowed_WithoutAdminUser_Denies()
    {
        AdminSessionGate.IsAllowed(hasAdminUser: false, mfaVerified: true, mfaExempt: true).Should().BeFalse();
    }

    [Fact]
    public void IsAllowed_WithUnverifiedNonExemptAdmin_Denies()
    {
        AdminSessionGate.IsAllowed(hasAdminUser: true, mfaVerified: false, mfaExempt: false).Should().BeFalse();
    }

    [Fact]
    public void IsAllowed_WithVerifiedAdmin_Allows()
    {
        AdminSessionGate.IsAllowed(hasAdminUser: true, mfaVerified: true, mfaExempt: false).Should().BeTrue();
    }

    [Fact]
    public void IsAllowed_WithMfaExemptAdmin_Allows()
    {
        AdminSessionGate.IsAllowed(hasAdminUser: true, mfaVerified: false, mfaExempt: true).Should().BeTrue();
    }

    [Theory]
    [InlineData("AdminAccount", "Login", true)]
    [InlineData("AdminAccount", "VerifyMfa", true)]
    [InlineData("AdminAccount", "Logout", true)]
    [InlineData("adminaccount", "login", true)]
    [InlineData("AdminAccount", "Index", false)]
    [InlineData("Dashboard", "Login", false)]
    public void IsGateExempt_ExemptsOnlyAdminAccountAuthActions(string controllerName, string actionName, bool expected)
    {
        AdminSessionGate.IsGateExempt(controllerName, actionName).Should().Be(expected);
    }

    [Theory]
    [InlineData("")]
    [InlineData("not-a-jwt")]
    [InlineData("header.payload.signature.extra")]
    [InlineData("header.!.signature")]
    public void TryReadMfaEnrollmentRestricted_WithEmptyOrMalformedToken_ReturnsFalse(string jwt)
    {
        AdminSessionGate.TryReadMfaEnrollmentRestricted(jwt).Should().BeFalse();
    }

    [Fact]
    public void TryReadMfaEnrollmentRestricted_WithoutClaim_ReturnsFalse()
    {
        AdminSessionGate.TryReadMfaEnrollmentRestricted(CreateToken("{\"sub\":\"test-subject\"}")).Should().BeFalse();
    }

    [Fact]
    public void TryReadMfaEnrollmentRestricted_WithTrueClaim_ReturnsTrue()
    {
        AdminSessionGate.TryReadMfaEnrollmentRestricted(CreateToken("{\"mfa_enrollment_only\":\"true\"}")).Should().BeTrue();
    }

    private static string CreateToken(string payload)
    {
        static string Encode(string value)
            => Convert.ToBase64String(Encoding.UTF8.GetBytes(value))
                .TrimEnd('=')
                .Replace('+', '-')
                .Replace('/', '_');

        return string.Concat(Encode("{}"), ".", Encode(payload), ".test-signature");
    }
}
