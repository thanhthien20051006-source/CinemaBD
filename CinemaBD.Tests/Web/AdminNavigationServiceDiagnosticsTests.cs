using CinemaBD.Web.Core;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;

namespace CinemaBD.Tests.Web;

public sealed class AdminNavigationServiceDiagnosticsTests
{
    [Fact]
    public async Task MissingSessionContext_LogsTraceRouteAndPresenceFlagsWithoutSecrets()
    {
        var context = new DefaultHttpContext();
        context.TraceIdentifier = "trace-id-for-test";
        context.Request.Path = "/Admin/Movies";
        context.Session = new TestSession(new Dictionary<string, string?>
        {
            ["AdminToken"] = "secret-admin-token-value"
        });
        var accessor = new HttpContextAccessor { HttpContext = context };
        var capture = new RecordingLogger<AdminNavigationService>();
        var client = new HttpClient(new StaticHandler(new HttpResponseMessage(System.Net.HttpStatusCode.Forbidden)));
        var service = new AdminNavigationService(client, accessor, capture);

        var result = await service.GetAccessAsync();

        result.Can("phim").Should().BeFalse();
        capture.Messages.Should().ContainSingle();
        var message = capture.Messages.Single();
        message.Should().Contain("TraceId=trace-id-for-test")
            .And.Contain("Route=/Admin/Movies")
            .And.Contain("HasAdminToken=True")
            .And.Contain("HasAdminId=False")
            .And.Contain("HttpStatus=")
            .And.NotContain("secret-admin-token-value");
    }

    [Fact]
    public async Task ForbiddenLookup_LogsHttpStatusAndNoTokenOrUsername()
    {
        var context = new DefaultHttpContext();
        context.TraceIdentifier = "trace-forbidden";
        context.Request.Path = "/Admin/Dashboard";
        context.Session = new TestSession(new Dictionary<string, string?>
        {
            ["AdminToken"] = "secret-admin-token-value",
            ["AdminId"] = "42",
            ["AdminUser"] = "sensitive-username"
        });
        var accessor = new HttpContextAccessor { HttpContext = context };
        var capture = new RecordingLogger<AdminNavigationService>();
        var client = new HttpClient(new StaticHandler(new HttpResponseMessage(System.Net.HttpStatusCode.Forbidden)));
        var service = new AdminNavigationService(client, accessor, capture);

        await service.GetAccessAsync();

        capture.Messages.Should().ContainSingle();
        var message = capture.Messages.Single();
        message.Should().Contain("TraceId=trace-forbidden")
            .And.Contain("Route=/Admin/Dashboard")
            .And.Contain("HasAdminToken=True")
            .And.Contain("HasAdminId=True")
            .And.Contain("HttpStatus=403")
            .And.NotContain("secret-admin-token-value")
            .And.NotContain("sensitive-username");
    }

    private sealed class StaticHandler(HttpResponseMessage response) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            => Task.FromResult(response);
    }

    private sealed class TestSession(Dictionary<string, string?> values) : ISession
    {
        public bool IsAvailable => true;
        public string Id { get; } = Guid.NewGuid().ToString("N");
        public IEnumerable<string> Keys => values.Keys;
        public void Clear() => values.Clear();
        public Task CommitAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task LoadAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
        public void Remove(string key) => values.Remove(key);
        public void Set(string key, byte[] value) => values[key] = System.Text.Encoding.UTF8.GetString(value);
        public bool TryGetValue(string key, [System.Diagnostics.CodeAnalysis.MaybeNullWhen(false)] out byte[] value)
        {
            if (values.TryGetValue(key, out var stored) && stored is not null)
            {
                value = System.Text.Encoding.UTF8.GetBytes(stored);
                return true;
            }
            value = null;
            return false;
        }
    }

    private sealed class RecordingLogger<T> : ILogger<T>
    {
        private readonly ConcurrentQueue<string> _messages = new();
        public IReadOnlyCollection<string> Messages => _messages.ToArray();
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
        public bool IsEnabled(LogLevel logLevel) => logLevel == LogLevel.Information;
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            if (IsEnabled(logLevel))
                _messages.Enqueue(formatter(state, exception));
        }
    }
}
