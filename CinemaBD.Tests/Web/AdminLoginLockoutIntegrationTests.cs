using CinemaBD.Api.Middleware;
using CinemaBD.Application.Exceptions;
using CinemaBD.Web.Models;
using CinemaBD.Web.Services;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit;

namespace CinemaBD.Tests.Web;

public sealed class AdminLoginLockoutIntegrationTests
{
    [Fact]
    public async Task Middleware_TemporaryLock_Returns423BodyReadableByWebAndAndroidAndRetryAfter()
    {
        var end = DateTime.UtcNow.AddMinutes(2).AddSeconds(1);
        var context = await InvokeMiddlewareAsync(new AccountLockedException(end), "/api/admin/auth/login", "POST");

        context.Response.StatusCode.Should().Be(423);
        int.Parse(context.Response.Headers.RetryAfter.ToString()).Should().BeInRange(1, 181);
        context.Response.ContentType.Should().StartWith("application/json");

        var body = await ReadBodyAsync(context);
        body.RootElement.GetProperty("success").GetBoolean().Should().BeFalse();
        body.RootElement.GetProperty("message").GetString().Should().Contain("khoảng 3 phút");
        body.RootElement.GetProperty("data").GetProperty("remainingMinutes").GetInt32().Should().Be(3);
        body.RootElement.ToString().Should().NotContain("LockoutEndUtc");
        body.RootElement.ToString().Should().NotContain("username");

        var webResponse = JsonSerializer.Deserialize<CinemaBD.Web.Models.ApiResponse<CinemaApiClient.LockedLoginData>>(body.RootElement.GetRawText(), WebJsonOptions())!;
        webResponse.Success.Should().BeFalse();
        webResponse.Message.Should().Contain("đăng nhập lại");
        webResponse.Data!.RemainingMinutes.Should().Be(3);

        // Android's NetworkErrorParser reads message/detail/title and ignores unknown fields.
        var androidShape = JsonDocument.Parse(body.RootElement.GetRawText());
        var androidMessage = androidShape.RootElement
            .EnumerateObject()
            .FirstOrDefault(x => x.Name is "message" or "Message" or "detail" or "Detail" or "title" or "Title")
            .Value.GetString();
        androidMessage.Should().Be(webResponse.Message);
    }

    [Fact]
    public async Task Middleware_LockLogContainsNoUsernameOrExceptionDetails()
    {
        var logger = new RecordingLogger<ExceptionHandlingMiddleware>();
        var context = new DefaultHttpContext();
        context.Request.Path = "/api/admin/auth/login";
        context.Request.Method = "POST";
        context.Response.Body = new MemoryStream();
        var middleware = new ExceptionHandlingMiddleware(_ => throw new AccountLockedException(DateTime.UtcNow.AddMinutes(5)), logger);

        await middleware.InvokeAsync(context);

        logger.Entries.Should().ContainSingle();
        logger.Entries[0].Level.Should().Be(LogLevel.Information);
        logger.Entries[0].Exception.Should().BeNull();
        logger.Entries[0].Message.Should().NotContain("locked_admin");
        logger.Entries[0].Message.Should().NotContain("Account is locked");
    }

    [Fact]
    public async Task Middleware_ManualLock_Returns423WithoutDurationOrRetryAfter()
    {
        var context = await InvokeMiddlewareAsync(new AccountLockedException(DateTime.MaxValue), "/api/admin/auth/login", "POST");

        context.Response.StatusCode.Should().Be(423);
        context.Response.Headers.ContainsKey("Retry-After").Should().BeFalse();
        var body = await ReadBodyAsync(context);
        body.RootElement.GetProperty("message").GetString().Should().Contain("liên hệ quản trị viên");
        body.RootElement.GetProperty("data").ValueKind.Should().Be(JsonValueKind.Null);
        body.RootElement.ToString().Should().NotContain("phút");
    }

    [Theory]
    [InlineData("/api/admin/auth/mfa/verify", "POST")]
    [InlineData("/api/admin/auth/refresh", "POST")]
    [InlineData("/api/admin/auth/login", "GET")]
    public async Task Middleware_AccountLockExceptionOutsideAdminLoginScope_KeepsExistingGeneric400Behavior(string path, string method)
    {
        var context = await InvokeMiddlewareAsync(new AccountLockedException(DateTime.MaxValue), path, method);

        context.Response.StatusCode.Should().Be(StatusCodes.Status400BadRequest);
        var body = await ReadBodyAsync(context);
        body.RootElement.GetProperty("detail").GetString().Should().Be("Trạng thái hiện tại không cho phép thực hiện yêu cầu này.");
    }

    [Fact]
    public async Task CinemaApiClient_Parses423ApiResponseAsLockedAdminLogin()
    {
        var http = new HttpClient(new StaticResponseHandler(() => new HttpResponseMessage((HttpStatusCode)423)
        {
            Content = JsonContent.Create(new CinemaBD.Api.Contracts.Common.ApiResponse<object>(false, "Tài khoản đang bị khóa tạm thời do đăng nhập sai quá nhiều lần. Vui lòng đợi khoảng 3 phút rồi đăng nhập lại.", new { remainingMinutes = 3 }))
        })) { BaseAddress = new Uri("https://unit.test/") };
        var client = new CinemaApiClient(http, new HttpContextAccessor(), NullLogger<CinemaApiClient>.Instance);

        var result = await client.AdminLoginAsync("admin", "not-a-real-secret");

        result.IsLocked.Should().BeTrue();
        result.Admin.Should().BeNull();
        result.ErrorMessage.Should().Contain("khoảng 3 phút");
    }

    private static async Task<DefaultHttpContext> InvokeMiddlewareAsync(Exception exception, string path, string method)
    {
        var context = new DefaultHttpContext();
        context.Request.Path = path;
        context.Request.Method = method;
        context.Response.Body = new MemoryStream();
        var middleware = new ExceptionHandlingMiddleware(_ => throw exception, NullLogger<ExceptionHandlingMiddleware>.Instance);
        await middleware.InvokeAsync(context);
        context.Response.Body.Position = 0;
        return context;
    }

    private static async Task<JsonDocument> ReadBodyAsync(HttpContext context)
        => await JsonDocument.ParseAsync(context.Response.Body);

    private static JsonSerializerOptions WebJsonOptions() => new(JsonSerializerDefaults.Web);

    private sealed class RecordingLogger<T> : ILogger<T>
    {
        public List<(LogLevel Level, string Message, Exception? Exception)> Entries { get; } = [];

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
        public bool IsEnabled(LogLevel logLevel) => true;
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
            => Entries.Add((logLevel, formatter(state, exception), exception));
    }

    private sealed class StaticResponseHandler(Func<HttpResponseMessage> createResponse) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            => Task.FromResult(createResponse());
    }

}
