
using ClinicManagementSystem.Web.Data;
using ClinicManagementSystem.Web.Data.Seeders;
using ClinicManagementSystem.Web.Models.Identity;
using ClinicManagementSystem.Web.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// =====================================
// 1. DATABASE CONFIGURATION
// =====================================

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection")));

// =====================================
// 2. ASP.NET CORE IDENTITY
// =====================================

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
        CookieSecurePolicy.Always;
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
builder.Services.AddScoped<IPatientService, PatientService>();
builder.Services.AddScoped<ISaleService, SaleService>();

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
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();

// =====================================
// 8. ROUTING
// =====================================

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

// =====================================
// 9. RUN APPLICATION
// =====================================

app.Run();
