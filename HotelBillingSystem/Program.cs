using HotelBillingSystem.Data;
using HotelBillingSystem.Hubs;
using HotelBillingSystem.Models;
using HotelBillingSystem.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// 1. Add DbContext (Supports Supabase PostgreSQL, XAMPP MySQL, with SQLite local fallback)
var supabaseConnectionString = builder.Configuration.GetConnectionString("SupabasePostgres");
var xamppConnectionString = builder.Configuration.GetConnectionString("XamppMySql");
var defaultConnectionString = builder.Configuration.GetConnectionString("DefaultConnection");

builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    bool dbConfigured = false;

    // Check if Supabase PostgreSQL password is set
    if (!string.IsNullOrEmpty(supabaseConnectionString) &&
        supabaseConnectionString.Contains("Host=") &&
        !supabaseConnectionString.Contains("YOUR_DATABASE_PASSWORD"))
    {
        try
        {
            options.UseNpgsql(supabaseConnectionString);
            dbConfigured = true;
            Console.WriteLine("[Supabase Database] Successfully connected to Supabase PostgreSQL.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Supabase Warning] Could not connect to Supabase PostgreSQL ({ex.Message}).");
        }
    }

    if (!dbConfigured && !string.IsNullOrEmpty(xamppConnectionString))
    {
        try
        {
            var serverVersion = ServerVersion.AutoDetect(xamppConnectionString);
            options.UseMySql(xamppConnectionString, serverVersion);
            dbConfigured = true;
            Console.WriteLine("[XAMPP MySQL Database] Successfully connected to XAMPP MySQL.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[XAMPP MySQL Warning] Could not connect to MySQL at localhost:3306 ({ex.Message}).");
        }
    }

    if (!dbConfigured)
    {
        options.UseSqlite(defaultConnectionString ?? "Data Source=pannaum_hotel.db");
        Console.WriteLine("[SQLite Database] Using local embedded SQLite database.");
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
