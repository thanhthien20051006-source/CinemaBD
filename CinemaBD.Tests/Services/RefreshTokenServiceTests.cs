using CinemaBD.Application.Interfaces;
using CinemaBD.Infrastructure.Persistence;
using CinemaBD.Infrastructure.Services;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;
using System.Security.Cryptography;

namespace CinemaBD.Tests.Services;

public sealed class RefreshTokenServiceTests
{
    private static AppDbContext CreateDb() => new(new DbContextOptionsBuilder<AppDbContext>()
        .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);

    [Fact]
    public async Task RotateAsync_GeneratesRandom256BitTokenAndStoresOnlyItsHashInStoreCall()
    {
        using var db = CreateDb();
        var oldToken = RefreshTokenService.CreateOpaqueToken();
        var oldHash = RefreshTokenService.HashToken(oldToken);
        var sessionId = Guid.NewGuid();
        db.Customers.Add(new LegacyCustomer { MaKH = "KHACHHANG", HoTen = "Customer", TKhoan = "user", MatKhau = "hash" });
        await db.SaveChangesAsync();
        var tokenService = new Mock<ITokenService>();
        tokenService.Setup(x => x.CreateToken(It.IsAny<CinemaBD.Domain.Entities.UserAccount>(), sessionId, It.IsAny<bool>())).Returns("rotated-access-token");
        var store = new Mock<IRefreshTokenStore>();
        byte[]? replacementHash = null;
        store.Setup(x => x.RotateAsync(It.Is<byte[]>(hash => hash.SequenceEqual(oldHash)), It.IsAny<byte[]>(), It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .Callback<byte[], byte[], Guid, string, string, DateTime, DateTime, CancellationToken>((_, hash, _, _, _, _, _, _) => replacementHash = hash.ToArray())
            .ReturnsAsync((byte[] _, byte[] _, Guid _, string _, string _, DateTime _, DateTime _, CancellationToken _) => new RefreshTokenStoreResult(RefreshTokenStoreOutcome.Rotated, sessionId, "KHACHHANG", "KHACHHANG"));
        var service = new RefreshTokenService(db, store.Object, tokenService.Object);

        var result = await service.RotateAsync(oldToken, "KHACHHANG", "KHACHHANG");

        RefreshTokenService.HashToken(result.RefreshToken).Length.Should().Be(32);
        replacementHash.Should().NotBeNull().And.HaveCount(32);
        replacementHash.Should().Equal(RefreshTokenService.HashToken(result.RefreshToken));
    }

    [Fact]
    public async Task RotateAsync_UsesPresentedTokenHashAndAccountScope()
    {
        using var db = CreateDb();
        var token = RefreshTokenService.CreateOpaqueToken();
        var expectedHash = RefreshTokenService.HashToken(token);
        var store = new Mock<IRefreshTokenStore>();
        byte[]? capturedHash = null;
        store.Setup(x => x.RotateAsync(It.IsAny<byte[]>(), It.IsAny<byte[]>(), It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .Callback<byte[], byte[], Guid, string, string, DateTime, DateTime, CancellationToken>((hash, _, _, _, _, _, _, _) => capturedHash = hash.ToArray())
            .ReturnsAsync(new RefreshTokenStoreResult(RefreshTokenStoreOutcome.Invalid, null));
        var service = new RefreshTokenService(db, store.Object, Mock.Of<ITokenService>());

        await service.Invoking(x => x.RotateAsync(token, "Admin", "27")).Should().ThrowAsync<UnauthorizedAccessException>();

        store.Verify(x => x.RotateAsync(It.IsAny<byte[]>(), It.IsAny<byte[]>(), It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()), Times.Once);
        capturedHash.Should().Equal(expectedHash);
    }

    [Fact]
    public async Task RotateAsync_RejectsMalformedTokensBeforeCallingStore()
    {
        using var db = CreateDb();
        var store = new Mock<IRefreshTokenStore>(MockBehavior.Strict);
        var service = new RefreshTokenService(db, store.Object, Mock.Of<ITokenService>());

        var act = () => service.RotateAsync("not-a-token", "KHACHHANG", "KH001");

        await act.Should().ThrowAsync<UnauthorizedAccessException>();
        store.VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData(RefreshTokenStoreOutcome.Invalid)]
    [InlineData(RefreshTokenStoreOutcome.Reused)]
    public async Task RotateAsync_DoesNotReturnReplacementOnNonSuccess(RefreshTokenStoreOutcome outcome)
    {
        using var db = CreateDb();
        var store = new Mock<IRefreshTokenStore>();
        store.Setup(x => x.RotateAsync(It.IsAny<byte[]>(), It.IsAny<byte[]>(), It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new RefreshTokenStoreResult(outcome, Guid.NewGuid()));
        var service = new RefreshTokenService(db, store.Object, Mock.Of<ITokenService>());

        var act = () => service.RotateAsync(RefreshTokenService.CreateOpaqueToken(), "KHACHHANG", "KH001");

        await act.Should().ThrowAsync<UnauthorizedAccessException>();
    }
}
