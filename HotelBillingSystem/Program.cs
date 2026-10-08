using HotelBillingSystem.Data;
using HotelBillingSystem.Hubs;
using HotelBillingSystem.Models;
using HotelBillingSystem.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// 1. Add DbContext (Supports XAMPP MySQL with local fallback)
var xamppConnectionString = builder.Configuration.GetConnectionString("XamppMySql");
var defaultConnectionString = builder.Configuration.GetConnectionString("DefaultConnection");

builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    bool mySqlConfigured = false;

    if (!string.IsNullOrEmpty(xamppConnectionString))
    {
        try
        {
            // AutoDetect MySQL / MariaDB Server Version
            var serverVersion = ServerVersion.AutoDetect(xamppConnectionString);
            options.UseMySql(xamppConnectionString, serverVersion);
            mySqlConfigured = true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[XAMPP MySQL Warning] Could not connect to MySQL at localhost:3306 ({ex.Message}). Falling back to SQLite.");
        }
    }

    if (!mySqlConfigured)
    {
        options.UseSqlite(defaultConnectionString ?? "Data Source=pannaum_hotel.db");
    }
});

// 2. Add ASP.NET Identity
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    options.Password.RequireDigit = false;
    options.Password.RequireLowercase = false;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequireUppercase = false;
    options.Password.RequiredLength = 4;
})
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.AccessDeniedPath = "/Account/AccessDenied";
});

// 3. Add Custom Application Services
builder.Services.AddScoped<IQrCodeService, QrCodeService>();
builder.Services.AddScoped<ITableService, TableService>();
builder.Services.AddScoped<IMenuService, MenuService>();
builder.Services.AddScoped<IKitchenAssignmentService, KitchenAssignmentService>();
builder.Services.AddScoped<IOrderService, OrderService>();
builder.Services.AddScoped<IBillingService, BillingService>();
builder.Services.AddScoped<IReportService, ReportService>();
builder.Services.AddScoped<IAuditLogService, AuditLogService>();

// 4. Add SignalR & Session
builder.Services.AddSignalR();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromHours(4);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

builder.Services.AddControllersWithViews();

var app = builder.Build();

// Seed Database
using (var scope = app.Services.CreateScope())
{
    await DbInitializer.SeedAsync(scope.ServiceProvider);
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

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.MapHub<HotelHub>("/hotelHub");

app.Run();
