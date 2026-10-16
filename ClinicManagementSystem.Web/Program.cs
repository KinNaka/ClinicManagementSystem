
using ClinicManagementSystem.Web.Data;
using ClinicManagementSystem.Web.Data.Seeders;
using ClinicManagementSystem.Web.Models.Identity;
using ClinicManagementSystem.Web.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
var builder = WebApplication.CreateBuilder(new WebApplicationOptions
{
    Args = args,
    ContentRootPath =
        Microsoft.Extensions.Hosting.WindowsServices.WindowsServiceHelpers.IsWindowsService()
            ? AppContext.BaseDirectory
            : Directory.GetCurrentDirectory()
});

builder.Host.UseWindowsService(options =>
{
    options.ServiceName = "LamThaoClinic";
});
var allowInternalHttp = builder.Configuration.GetValue<bool>("Hosting:AllowHttp");
var databaseConnection = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Chưa cấu hình DefaultConnection.");
if (builder.Environment.IsDevelopment())
    databaseConnection = await DevelopmentLocalDbConnection.ResolveAsync(databaseConnection);

// =====================================
// 1. DATABASE CONFIGURATION
// =====================================

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(databaseConnection));

// =====================================
// 2. ASP.NET CORE IDENTITY
// =====================================
builder.Services.AddScoped<IDashboardService, DashboardService>();
builder.Services
    .AddIdentity<ApplicationUser, IdentityRole>(options =>
    {
        options.Password.RequireDigit = true;
        options.Password.RequireLowercase = true;
        options.Password.RequireUppercase = true;
        options.Password.RequireNonAlphanumeric = true;
        options.Password.RequiredLength = 8;

        options.User.RequireUniqueEmail = true;
    })
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.AccessDeniedPath = "/Account/AccessDenied";

    options.ExpireTimeSpan = TimeSpan.FromHours(8);
    options.SlidingExpiration = true;

    options.Cookie.HttpOnly = true;
    options.Cookie.SecurePolicy =
        allowInternalHttp ? CookieSecurePolicy.SameAsRequest : CookieSecurePolicy.Always;
});

builder.Services.Configure<IdentityOptions>(options =>
{
    options.Lockout.DefaultLockoutTimeSpan =
        TimeSpan.FromMinutes(15);

    options.Lockout.MaxFailedAccessAttempts = 5;
    options.Lockout.AllowedForNewUsers = true;
});


// =====================================
// 3. MVC
// =====================================


builder.Services.AddControllersWithViews(options =>
{
    var policy = new Microsoft.AspNetCore.Authorization
        .AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build();

    options.Filters.Add(
        new Microsoft.AspNetCore.Mvc.Authorization.AuthorizeFilter(policy));
});


// =====================================
// 4. APPLICATION SERVICES
// =====================================

builder.Services.AddScoped<IProductCategoryService, ProductCategoryService>();
builder.Services.AddScoped<IProductService, ProductService>();
builder.Services.AddScoped<IInventoryService, InventoryService>();
builder.Services.AddSingleton<VietnamAddressService>();
builder.Services.AddMemoryCache();
builder.Services.AddScoped<KiotVietCustomerReader>();
builder.Services.AddScoped<PatientImportService>();
builder.Services.AddScoped<KiotVietProductReader>();
builder.Services.AddScoped<ProductImportService>();
builder.Services.AddScoped<KiotVietSaleReader>();
builder.Services.AddScoped<SaleImportService>();
builder.Services.AddScoped<IPatientService, PatientService>();
builder.Services.AddScoped<ISaleService, SaleService>();
builder.Services.AddScoped<AppointmentService>();
builder.Services.Configure<SecurityStampValidatorOptions>(options =>
{
    options.ValidationInterval = TimeSpan.Zero;
});

// =====================================
// 5. BUILD APPLICATION
// =====================================

var app = builder.Build();

// =====================================
// 6. SEED IDENTITY DATA
// =====================================

using (var scope = app.Services.CreateScope())
{
    await IdentitySeeder.SeedAsync(
        scope.ServiceProvider,
        scope.ServiceProvider.GetRequiredService<IConfiguration>());
}

// =====================================
// 7. HTTP REQUEST PIPELINE
// =====================================

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    if (!allowInternalHttp) app.UseHsts();
}

if (!allowInternalHttp) app.UseHttpsRedirection();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();

// =====================================
// 8. ROUTING
// =====================================

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Dashboard}/{action=Index}/{id?}")
    .WithStaticAssets();

// =====================================
// 9. RUN APPLICATION
// =====================================

app.Run();
