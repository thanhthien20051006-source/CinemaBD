using CinemaBD.Application.Interfaces;
using CinemaBD.Infrastructure.DependencyInjection;
using CinemaBD.Infrastructure.Persistence;
using CinemaBD.Infrastructure.Security;
using CinemaBD.Infrastructure.Services;
using CinemaBD.Web.Configurations;
using CinemaBD.Web.Core;
using CinemaBD.Web.Infrastructure.Notifications;
using CinemaBD.Web.Infrastructure.Payments;
using CinemaBD.Web.Services;
using CinemaBD.Web.Hubs;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();
builder.Services.AddSignalR();
builder.Services.AddProblemDetails();
var dataProtectionKeysDir = builder.Configuration["DataProtection:KeyDirectory"]
    ?? Path.Combine(builder.Environment.ContentRootPath, "App_Data", "DataProtection-Keys");
builder.Services.AddDataProtection()
    .SetApplicationName("CinemaBD")
    .PersistKeysToFileSystem(new DirectoryInfo(dataProtectionKeysDir));
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddScoped<DatabaseInitializer>();
var jwtKey = builder.Configuration["Jwt:Key"];
var jwtIssuer = builder.Configuration["Jwt:Issuer"];
var jwtAudience = builder.Configuration["Jwt:Audience"];
var jwtBearerConfigured = !string.IsNullOrWhiteSpace(jwtKey)
    && !jwtKey.StartsWith("CHANGE_ME", StringComparison.OrdinalIgnoreCase)
    && !string.IsNullOrWhiteSpace(jwtIssuer)
    && !string.IsNullOrWhiteSpace(jwtAudience);
if (!jwtBearerConfigured)
    throw new InvalidOperationException("Jwt:Key, Jwt:Issuer and Jwt:Audience must be configured for Web API routes.");
if (Encoding.UTF8.GetByteCount(jwtKey!) < 32)
    throw new InvalidOperationException("Jwt:Key must contain at least 32 bytes.");
var qrSigningKey = builder.Configuration["Security:QrSigningKey"];
if (string.IsNullOrWhiteSpace(qrSigningKey) || qrSigningKey.StartsWith("CHANGE_ME", StringComparison.OrdinalIgnoreCase)
    || Encoding.UTF8.GetByteCount(qrSigningKey) < 32)
    throw new InvalidOperationException("Security:QrSigningKey must be configured with at least 32 bytes of secret material.");

var requireSecureCookies = builder.Configuration.GetValue<bool>("Security:RequireSecureCookies", true);
var cookieSecurePolicy = requireSecureCookies ? CookieSecurePolicy.Always : CookieSecurePolicy.SameAsRequest;

builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.SecurePolicy = cookieSecurePolicy;
    options.Cookie.SameSite = SameSiteMode.Lax;
});

builder.Services.AddDataProtection()
    .PersistKeysToFileSystem(new DirectoryInfo(Path.Combine(builder.Environment.ContentRootPath, "DataProtectionKeys")))
    .SetApplicationName("CinemaBD.Web");

builder.Services.Configure<EmailSettings>(builder.Configuration.GetSection("EmailSettings"));

builder.Services.AddHttpContextAccessor();
builder.Services.AddSingleton<IQrCodeSecurityService, HmacQrCodeSecurityService>();
builder.Services.AddHttpClient<IAuthCoreService, ApiAuthCoreService>(client =>
{
    client.BaseAddress = new Uri(builder.Configuration["ApiSettings:BaseUrl"] ?? "http://localhost:5188/");
});
builder.Services.AddHttpClient<IBookingCoreService, ApiBookingCoreService>(client =>
{
    client.BaseAddress = new Uri(builder.Configuration["ApiSettings:BaseUrl"] ?? "http://localhost:5188/");
});
builder.Services.AddHttpClient<IAdminLegacyReadService, AdminLegacyReadService>(client =>
{
    client.BaseAddress = new Uri(builder.Configuration["ApiSettings:BaseUrl"] ?? "http://localhost:5188/");
});
builder.Services.AddHttpClient<IAdminNavigationService, AdminNavigationService>(client =>
{
    client.BaseAddress = new Uri(builder.Configuration["ApiSettings:BaseUrl"] ?? "http://localhost:5188/");
});
builder.Services.Configure<MomoOptionModel>(builder.Configuration.GetSection("MomoAPI"));
builder.Services.AddHttpClient<IMomoService, MomoService>();
builder.Services.AddScoped<ITicketPdfService, TicketPdfService>();
builder.Services.AddScoped<IInvoiceNotificationService, InvoiceNotificationService>();
builder.Services.AddHttpClient();

builder.Services.AddTransient<ApiSessionTokenRefreshHandler>();
builder.Services.AddHttpClient<CinemaApiClient>(client =>
{
    client.BaseAddress = new Uri(builder.Configuration["ApiSettings:BaseUrl"] ?? "http://localhost:5188/");
}).AddHttpMessageHandler<ApiSessionTokenRefreshHandler>();

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = "Cookies";
    options.DefaultSignInScheme = "Cookies";
    options.DefaultChallengeScheme = "Google";
})
.AddCookie("Cookies", options =>
{
    options.Cookie.HttpOnly = true;
    options.Cookie.SecurePolicy = cookieSecurePolicy;
    options.Cookie.SameSite = SameSiteMode.Lax;
    options.ExpireTimeSpan = TimeSpan.FromHours(8);
    options.SlidingExpiration = true;
})
.AddGoogle("Google", options =>
{
    options.ClientId = builder.Configuration["GoogleAuth:ClientId"] ?? "dummy";
    options.ClientSecret = builder.Configuration["GoogleAuth:ClientSecret"] ?? "dummy";
});

builder.Services.AddAuthentication("Cookies")
    .AddJwtBearer(JwtBearerDefaults.AuthenticationScheme, options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            RequireExpirationTime = true,
            RequireSignedTokens = true,
            ValidIssuer = jwtIssuer,
            ValidAudience = jwtAudience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey!)),
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

                var revokedTokens = context.HttpContext.RequestServices.GetRequiredService<IRevokedTokenService>();
                if (await revokedTokens.IsRevokedAsync(jti, context.HttpContext.RequestAborted))
                    context.Fail("JWT has been revoked.");
            }
        };
    });

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    await scope.ServiceProvider.GetRequiredService<DatabaseInitializer>().InitializeAsync();
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler(errorApp =>
    {
        errorApp.Run(async context =>
        {
            context.Response.StatusCode = StatusCodes.Status500InternalServerError;
            context.Response.ContentType = "text/plain; charset=utf-8";
            await context.Response.WriteAsync("Đã xảy ra lỗi khi xử lý yêu cầu. Vui lòng thử lại sau.");
        });
    });
    app.UseHsts();
}
app.UseHttpsRedirection();

app.Use(async (context, next) =>
{
    context.Response.Headers.Append("X-Frame-Options", "DENY");
    context.Response.Headers.Append("X-Content-Type-Options", "nosniff");
    context.Response.Headers.Append("Referrer-Policy", "no-referrer");
    await next();
});

app.UseStaticFiles();
app.UseRouting();
app.UseSession();
app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/Admin", () => Results.Redirect("/Admin/Dashboard"));
app.MapGet("/Admin/Statistics", () => Results.Redirect("/Admin/AdminThongKe"));

app.MapControllers();
app.MapHub<SeatHub>("/hubs/seats");
app.MapControllerRoute(
    name: "areas",
    pattern: "{area:exists}/{controller=Dashboard}/{action=Index}/{id?}");

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();

namespace CinemaBD.Web
{
    public partial class Program { }
}



