using CinemaBD.Infrastructure.Security;
using FluentAssertions;

namespace CinemaBD.Tests.Security;

public class SecurityMetadataSanitizerTests
{
    public static TheoryData<string> SensitiveValues => new()
    {
        "access token: eyJhbGciOiJIUzI1NiJ9.eyJzdWIiOiIxMjMifQ.signature",
        "refresh secret = abcdefghijklmnopqrstuvwxyz0123456789",
        "password=correct-horse-battery-staple",
        "credential hash 0123456789abcdef0123456789abcdef0123456789abcdef0123456789abcdef",
        "IDENTITY:AQAAAAIAAYagAAAAEJ2XKq0yqfQ4hU/sensitiveHashMaterial",
        "safe description " + new string('A', 1001) + " token=hidden-value"
    };

    [Theory]
    [MemberData(nameof(SensitiveValues))]
    public void SanitizeDetails_RejectsSensitiveValuesWithoutEchoingInput(string value)
    {
        var action = () => SecurityMetadataSanitizer.SanitizeDetails(value);

        var exception = action.Should().Throw<ArgumentException>().Which;
        exception.Message.Should().NotContain(value);
        exception.Message.Should().NotContain("hidden-value");
    }

    [Theory]
    [MemberData(nameof(SensitiveValues))]
    public void SanitizeRevocationReason_RejectsSensitiveValuesWithoutEchoingInput(string value)
    {
        var action = () => SecurityMetadataSanitizer.SanitizeRevocationReason(value);

        var exception = action.Should().Throw<ArgumentException>().Which;
        exception.Message.Should().NotContain(value);
        exception.Message.Should().NotContain("hidden-value");
    }

    [Theory]
    [InlineData("login_failed")]
    [InlineData("password_changed")]
    [InlineData("token_expired")]
    [InlineData("manual logout")]
    public void SanitizeDetails_AllowsOrdinaryEventDescriptions(string value)
    {
        SecurityMetadataSanitizer.SanitizeDetails(value).Should().Be(value);
    }

    [Theory]
    [InlineData("login_failed")]
    [InlineData("password_changed")]
    [InlineData("token_expired")]
    [InlineData("manual logout")]
    public void SanitizeRevocationReason_AllowsOrdinaryReasons(string value)
    {
        SecurityMetadataSanitizer.SanitizeRevocationReason(value).Should().Be(value);
    }

    [Fact]
    public void SanitizeDetails_PreservesNullAndEnforcesSchemaLength()
    {
        SecurityMetadataSanitizer.SanitizeDetails(null).Should().BeNull();
        SecurityMetadataSanitizer.SanitizeDetails(string.Empty).Should().BeEmpty();
        var detailsAtLimit = string.Concat(Enumerable.Repeat("safe ", 200));
        var detailsOverLimit = string.Concat(Enumerable.Repeat("safe ", 201));
        SecurityMetadataSanitizer.SanitizeDetails(detailsAtLimit).Should().HaveLength(1000);
        SecurityMetadataSanitizer.SanitizeDetails(detailsOverLimit).Should().Be(detailsOverLimit[..1000]);
    }

    [Fact]
    public void SanitizeRevocationReason_PreservesNullAndEnforcesSchemaLength()
    {
        SecurityMetadataSanitizer.SanitizeRevocationReason(null).Should().BeNull();
        SecurityMetadataSanitizer.SanitizeRevocationReason(string.Empty).Should().BeEmpty();
        var reasonAtLimit = string.Concat(Enumerable.Repeat("safe ", 25)) + "abc";
        var reasonOverLimit = reasonAtLimit + "de";
        SecurityMetadataSanitizer.SanitizeRevocationReason(reasonAtLimit).Should().HaveLength(128);
        SecurityMetadataSanitizer.SanitizeRevocationReason(reasonOverLimit).Should().Be(reasonOverLimit[..128]);
    }
}
