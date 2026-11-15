using CinemaBD.Api.Controllers;
using CinemaBD.Application.Interfaces;
using CinemaBD.Domain.Entities;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace CinemaBD.Tests.Controllers;

public class DemoConfirmProductionTests
{
    [Theory]
    [InlineData("Production")]
    [InlineData("Staging")]
    public async Task DemoConfirm_WhenEnvironmentIsNotDevelopment_ShouldReturnNotFound(string environmentName)
    {
        // Arrange
        var paymentServiceMock = new Mock<IPaymentService>();
        var envMock = new Mock<IWebHostEnvironment>();
        envMock.Setup(x => x.EnvironmentName).Returns(environmentName);

        var controller = new PaymentsController(paymentServiceMock.Object, envMock.Object);

        // Act
        var result = await controller.DemoConfirm("TXN12345", "MOMO", CancellationToken.None);

        // Assert
        result.Should().BeOfType<NotFoundResult>();
        paymentServiceMock.Verify(x => x.ConfirmDemoPaymentAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task DemoConfirm_WhenEnvironmentIsDevelopment_ShouldCallPaymentServiceAndReturnOk()
    {
        // Arrange
        var paymentServiceMock = new Mock<IPaymentService>();
        paymentServiceMock.Setup(x => x.ConfirmDemoPaymentAsync("TXN_DEV_01", "MOMO", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PaymentCallbackResult
            {
                Success = true,
                SignatureValid = true,
                TransactionRef = "TXN_DEV_01",
                ResponseCode = "00",
                Message = "Thành công"
            });

        var envMock = new Mock<IWebHostEnvironment>();
        envMock.Setup(x => x.EnvironmentName).Returns("Development");

        var controller = new PaymentsController(paymentServiceMock.Object, envMock.Object);

        // Act
        var result = await controller.DemoConfirm("TXN_DEV_01", "MOMO", CancellationToken.None);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        paymentServiceMock.Verify(x => x.ConfirmDemoPaymentAsync("TXN_DEV_01", "MOMO", It.IsAny<CancellationToken>()), Times.Once);
    }
}
