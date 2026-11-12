using CinemaBD.Infrastructure.Security;
using FluentAssertions;
using System.Security.Cryptography;
using System.Text;

namespace CinemaBD.Tests.Security;

public class Md5PasswordHasherTests
{
    private readonly Md5PasswordHasher _hasher = new();

    [Fact]
    public void Hash_ShouldStartWithIdentityPrefix_AndVerifySuccessfully()
    {
        var password = "SecurePassword123!";
        var hash = _hasher.Hash(password);

        hash.Should().StartWith("IDENTITY:");
        _hasher.Verify(password, hash).Should().BeTrue();
        _hasher.Verify("WrongPassword", hash).Should().BeFalse();
    }

    [Fact]
    public void Verify_WithLegacyMd5Hash_ShouldVerifySuccessfully()
    {
        var password = "password123";
        using var md5 = MD5.Create();
        var md5Bytes = md5.ComputeHash(Encoding.UTF8.GetBytes(password));
        var md5Hash = BitConverter.ToString(md5Bytes).Replace("-", "").ToLowerInvariant();

        _hasher.Verify(password, md5Hash).Should().BeTrue();
        _hasher.Verify("wrong", md5Hash).Should().BeFalse();
    }

    [Fact]
    public void Verify_WithPlaintext_ShouldReturnFalse_PlaintextBranchRemoved()
    {
        var password = "plainTextPasswordDemo";
        _hasher.Verify(password, password).Should().BeFalse();
        _hasher.Verify("wrong", password).Should().BeFalse();
    }

    [Fact]
    public void NeedsRehash_ShouldReturnFalse_ForIdentityHash()
    {
        var hash = _hasher.Hash("anyPassword");
        _hasher.NeedsRehash(hash).Should().BeFalse();
    }

    [Theory]
    [InlineData("202cb962ac59075b964b07152d234b70")]
    [InlineData("plainText")]
    [InlineData("")]
    [InlineData(null)]
    public void NeedsRehash_ShouldReturnTrue_ForNonIdentityHash(string? storedHash)
    {
        _hasher.NeedsRehash(storedHash!).Should().BeTrue();
    }
}
