using CinemaBD.Api.Extensions;
using CinemaBD.Infrastructure.DependencyInjection;
using CinemaBD.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddProblemDetails();
builder.Services.AddScoped<CinemaBD.Api.Filters.AdminPermissionAuthorizationFilter>();
builder.Services.AddScoped<CinemaBD.Api.Filters.PasswordChangeRestrictionFilter>();
builder.Services.AddControllers(options =>
{
    options.Filters.AddService<CinemaBD.Api.Filters.AdminPermissionAuthorizationFilter>();
    options.Filters.AddService<CinemaBD.Api.Filters.PasswordChangeRestrictionFilter>();
});
builder.Services.AddCors(options => options.AddPolicy("WebClients", policy =>
{
    var origins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? Array.Empty<string>();
    foreach (var origin in origins)
    {
        if (!Uri.TryCreate(origin, UriKind.Absolute, out var uri)
            || (uri.AbsolutePath != "/" || !string.IsNullOrEmpty(uri.Query) || !string.IsNullOrEmpty(uri.Fragment))
            || (!string.Equals(uri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase)
                && !builder.Environment.IsDevelopment()))
            throw new InvalidOperationException("Cors:AllowedOrigins must contain valid HTTPS origins outside Development.");
    }

    if (origins.Length > 0)
    {
        policy.WithOrigins(origins)
            .WithMethods("GET", "POST", "PUT", "DELETE", "OPTIONS")
            .WithHeaders("Authorization", "Content-Type", "X-Return-Url");
    }
}));
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy("auth", context => RateLimitPartition.GetFixedWindowLimiter(
        context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
        _ => new FixedWindowRateLimiterOptions { PermitLimit = 10, Window = TimeSpan.FromMinutes(1), QueueLimit = 0, AutoReplenishment = true }));
    options.AddPolicy("transactions", context => RateLimitPartition.GetFixedWindowLimiter(
        context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
        _ => new FixedWindowRateLimiterOptions { PermitLimit = 20, Window = TimeSpan.FromMinutes(1), QueueLimit = 0, AutoReplenishment = true }));
    options.OnRejected = async (context, cancellationToken) =>
    {
        var problemDetails = new ProblemDetails
        {
            Status = StatusCodes.Status429TooManyRequests,
            Title = "Quá nhiều yêu cầu",
            Detail = "Vui lòng thử lại sau."
        };
        problemDetails.Extensions["traceId"] = context.HttpContext.TraceIdentifier;
        context.HttpContext.Response.ContentType = "application/problem+json";
        context.HttpContext.Response.Headers.RetryAfter = "60";
        await context.HttpContext.RequestServices.GetRequiredService<IProblemDetailsService>()
            .WriteAsync(new ProblemDetailsContext { HttpContext = context.HttpContext, ProblemDetails = problemDetails });
    };
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "CinemaBD API",
        Version = "v1",
        Description = "ASP.NET Core Web API migrated from the legacy MVC 5 CinemaBD project"
    });

    var securityScheme = new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Nhập JWT theo dạng: Bearer {your token}",
        Reference = new OpenApiReference
        {
            Type = ReferenceType.SecurityScheme,
            Id = JwtBearerDefaults.AuthenticationScheme
        }
    };

    options.AddSecurityDefinition(JwtBearerDefaults.AuthenticationScheme, securityScheme);
    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        { securityScheme, Array.Empty<string>() }
    });
});
builder.Services.AddInfrastructure(builder.Configuration);

var qrSigningKey = builder.Configuration["Security:QrSigningKey"];
if (string.IsNullOrWhiteSpace(qrSigningKey) || qrSigningKey.StartsWith("CHANGE_ME", StringComparison.OrdinalIgnoreCase)
    || Encoding.UTF8.GetByteCount(qrSigningKey) < 32)
    throw new InvalidOperationException("Security:QrSigningKey must be configured with at least 32 bytes of secret material.");

var jwtKey = builder.Configuration["Jwt:Key"];
if (string.IsNullOrWhiteSpace(jwtKey) || jwtKey.StartsWith("CHANGE_ME", StringComparison.OrdinalIgnoreCase))
    throw new InvalidOperationException("Jwt:Key must be configured with a strong secret.");

var jwtIssuer = builder.Configuration["Jwt:Issuer"];
var jwtAudience = builder.Configuration["Jwt:Audience"];
if (Encoding.UTF8.GetByteCount(jwtKey) < 32)
    throw new InvalidOperationException("Jwt:Key must contain at least 32 bytes.");
if (string.IsNullOrWhiteSpace(jwtIssuer) || string.IsNullOrWhiteSpace(jwtAudience))
    throw new InvalidOperationException("Jwt:Issuer and Jwt:Audience must be configured.");

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            RequireExpirationTime = true,
            ValidIssuer = jwtIssuer,
            ValidAudience = jwtAudience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
            RoleClaimType = System.Security.Claims.ClaimTypes.Role,
            ClockSkew = TimeSpan.Zero
        };
        options.Events = new JwtBearerEvents
        {
            OnTokenValidated = async context =>
            {
                var jti = context.Principal?.FindFirst(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Jti)?.Value;
                if (string.IsNullOrWhiteSpace(jti))
                {
                    context.Fail("JWT is missing its token identifier.");
                    return;
                }

                var revokedTokens = context.HttpContext.RequestServices
                    .GetRequiredService<CinemaBD.Application.Interfaces.IRevokedTokenService>();
                if (await revokedTokens.IsRevokedAsync(jti, context.HttpContext.RequestAborted))
                    context.Fail("JWT has been revoked.");
            }
        };
    });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", policy => policy.RequireClaim("account_type", "admin"));
    options.AddPolicy("NotPasswordChangeOnly", policy => policy.RequireAssertion(context =>
        !context.User.HasClaim("password_change_only", "true")));
});

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var databaseInitializer = scope.ServiceProvider.GetRequiredService<DatabaseInitializer>();
    await databaseInitializer.InitializeAsync();
}

app.UseGlobalExceptionHandling();
app.UseSwagger();
app.UseSwaggerUI();

var webWwwrootPath = Path.GetFullPath(Path.Combine(builder.Environment.ContentRootPath, "..", "CinemaBD.Web", "wwwroot"));
if (Directory.Exists(webWwwrootPath))
{
    app.UseStaticFiles(new StaticFileOptions
    {
        FileProvider = new Microsoft.Extensions.FileProviders.PhysicalFileProvider(webWwwrootPath)
    });
}
else
{
    app.Logger.LogWarning("Web wwwroot directory does not exist: {webWwwrootPath}", webWwwrootPath);
}

app.UseHttpsRedirection();
app.Use(async (context, next) =>
{
    context.Response.OnStarting(() =>
    {
        context.Response.Headers.XContentTypeOptions = "nosniff";
        context.Response.Headers.XFrameOptions = "DENY";
        context.Response.Headers["Referrer-Policy"] = "no-referrer";
        context.Response.Headers["Content-Security-Policy"] = context.Request.Path.StartsWithSegments("/swagger")
            ? "default-src 'self'; script-src 'self' 'unsafe-inline'; style-src 'self' 'unsafe-inline'; img-src 'self' data:; font-src 'self'; frame-ancestors 'none'; base-uri 'none'"
            : "default-src 'none'; frame-ancestors 'none'; base-uri 'none'";
        if (!app.Environment.IsDevelopment())
            context.Response.Headers.StrictTransportSecurity = "max-age=31536000; includeSubDomains";
        return Task.CompletedTask;
    });
    await next();
});
app.UseCors("WebClients");
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();

namespace CinemaBD.Api
{
    public partial class Program { }
}

