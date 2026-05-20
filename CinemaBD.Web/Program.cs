using CinemaBD.Web.Configurations;
using CinemaBD.Web.Core;
using CinemaBD.Web.Data;
using CinemaBD.Web.Infrastructure.Notifications;
using CinemaBD.Web.Infrastructure.Payments;
using CinemaBD.Web.Services;
using CinemaBD.Web.Hubs;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.DataProtection;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();
builder.Services.AddSignalR();
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

builder.Services.AddDataProtection()
    .PersistKeysToFileSystem(new DirectoryInfo(Path.Combine(builder.Environment.ContentRootPath, "DataProtectionKeys")))
    .SetApplicationName("CinemaBD.Web");

builder.Services.AddDbContext<CinemaDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection") ?? "Data Source=cinemabd.web.db"));

builder.Services.Configure<EmailSettings>(builder.Configuration.GetSection("EmailSettings"));

builder.Services.AddScoped<IAuthCoreService, AuthCoreService>();
builder.Services.AddScoped<IBookingCoreService, BookingCoreService>();
builder.Services.AddScoped<IAdminAuthCoreService, AdminAuthCoreService>();
builder.Services.AddScoped<IAdminDashboardCoreService, AdminDashboardCoreService>();
builder.Services.AddScoped<IAdminBookingCoreService, AdminBookingCoreService>();
builder.Services.AddScoped<IAdminMovieCoreService, AdminMovieCoreService>();
builder.Services.AddScoped<IAdminShowtimeCoreService, AdminShowtimeCoreService>();
builder.Services.AddScoped<IAdminComboCoreService, AdminComboCoreService>();
builder.Services.AddScoped<IAdminUserCoreService, AdminUserCoreService>();
builder.Services.AddHttpContextAccessor();
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

builder.Services.AddHttpClient<CinemaApiClient>(client =>
{
    client.BaseAddress = new Uri(builder.Configuration["ApiSettings:BaseUrl"] ?? "http://localhost:5188/");
});

builder.Services.AddAuthentication(options =>
{
    options.DefaultScheme = "Cookies";
    options.DefaultChallengeScheme = "Google";
})
.AddCookie("Cookies")
.AddGoogle("Google", options =>
{
    options.ClientId = builder.Configuration["GoogleAuth:ClientId"] ?? "dummy";
    options.ClientSecret = builder.Configuration["GoogleAuth:ClientSecret"] ?? "dummy";
});

var app = builder.Build();


using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<CinemaDbContext>();
    await db.Database.MigrateAsync();
    await SeedData.InitializeAsync(db);
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseSession();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHub<SeatHub>("/hubs/seats");
app.MapControllerRoute(
    name: "areas",
    pattern: "{area:exists}/{controller=Dashboard}/{action=Index}/{id?}");

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();


