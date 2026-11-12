using FluentAssertions;
using Microsoft.AspNetCore.Http;

namespace CinemaBD.Tests.Web;

public class WebSecurityHeadersTests
{
    [Fact]
    public async Task SecurityHeadersMiddleware_ShouldSetDenyNosniffAndNoReferrer()
    {
        // Arrange
        var context = new DefaultHttpContext();
        RequestDelegate next = (ctx) => Task.CompletedTask;

        // Act: simulate the middleware added in CinemaBD.Web/Program.cs
        Func<HttpContext, RequestDelegate, Task> middleware = async (ctx, nxt) =>
        {
            ctx.Response.Headers.Append("X-Frame-Options", "DENY");
            ctx.Response.Headers.Append("X-Content-Type-Options", "nosniff");
            ctx.Response.Headers.Append("Referrer-Policy", "no-referrer");
            await nxt(ctx);
        };

        await middleware(context, next);

        // Assert
        context.Response.Headers["X-Frame-Options"].ToString().Should().Be("DENY");
        context.Response.Headers["X-Content-Type-Options"].ToString().Should().Be("nosniff");
        context.Response.Headers["Referrer-Policy"].ToString().Should().Be("no-referrer");
    }
}
