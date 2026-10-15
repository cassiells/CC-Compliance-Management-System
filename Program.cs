using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.Cookies;
using DoComply.Models;
using DoComply.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorPages();
builder.Services.AddHttpClient();
// QuestPDF License & Font Configuration
QuestPDF.Settings.License = QuestPDF.Infrastructure.LicenseType.Community;
QuestPDF.Settings.UseSystemFonts = true;
QuestPDF.Settings.ThrowOnMissingFontFamilies = false;

builder.Services.AddScoped<ICloudinaryDocumentService, CloudinaryDocumentService>();
builder.Services.AddScoped<IBirTinVerificationService, BirTinVerificationService>();
builder.Services.AddScoped<ILookupTaxVerificationService, BirTinVerificationService>();
builder.Services.AddScoped<IReportPdfService, ReportPdfService>();
builder.Services.AddSingleton<IAltchaService, AltchaService>();
builder.Services.AddSingleton<ILoginLockoutService, LoginLockoutService>();

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

builder.Services.AddDbContext<DoComplyContext>(options =>
    options.UseSqlServer(connectionString));

builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromHours(4);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.SameSite = SameSiteMode.Lax;
});

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.LogoutPath = "/Account/Logout";
        options.AccessDeniedPath = "/Account/Login";
        options.ExpireTimeSpan = TimeSpan.FromHours(4);
        options.SlidingExpiration = true;
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Lax;
    });

builder.Services.AddAuthorization();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseSession();
app.UseAuthentication();
app.UseAuthorization();

// Global Anti-Bypass & No-Cache Middleware
app.Use(async (context, next) =>
{
    var path = context.Request.Path.Value ?? "";

    // Set No-Cache headers on all dynamic HTML responses to prevent Back-button restoring from browser cache
    if (!path.StartsWith("/css", StringComparison.OrdinalIgnoreCase) &&
        !path.StartsWith("/js", StringComparison.OrdinalIgnoreCase) &&
        !path.StartsWith("/images", StringComparison.OrdinalIgnoreCase) &&
        !path.StartsWith("/lib", StringComparison.OrdinalIgnoreCase) &&
        !path.StartsWith("/favicon", StringComparison.OrdinalIgnoreCase))
    {
        context.Response.Headers["Cache-Control"] = "no-cache, no-store, must-revalidate, max-age=0";
        context.Response.Headers["Pragma"] = "no-cache";
        context.Response.Headers["Expires"] = "0";
    }

    // Public endpoints that do not require authentication
    bool isPublic = path.StartsWith("/css", StringComparison.OrdinalIgnoreCase) ||
                    path.StartsWith("/js", StringComparison.OrdinalIgnoreCase) ||
                    path.StartsWith("/images", StringComparison.OrdinalIgnoreCase) ||
                    path.StartsWith("/lib", StringComparison.OrdinalIgnoreCase) ||
                    path.StartsWith("/favicon", StringComparison.OrdinalIgnoreCase) ||
                    path.Equals("/Account/Login", StringComparison.OrdinalIgnoreCase) ||
                    path.Equals("/Account/Logout", StringComparison.OrdinalIgnoreCase) ||
                    path.Equals("/", StringComparison.OrdinalIgnoreCase) ||
                    path.Equals("/Index", StringComparison.OrdinalIgnoreCase) ||
                    path.Equals("/Error", StringComparison.OrdinalIgnoreCase);

    if (!isPublic)
    {
        var sessionUserId = context.Session.GetString("UserId");
        bool isAuth = !string.IsNullOrEmpty(sessionUserId) || (context.User?.Identity?.IsAuthenticated == true);

        if (!isAuth)
        {
            // Unauthenticated attempt - prevent bypass and redirect to Login
            context.Response.Redirect("/Account/Login");
            return;
        }

        // Role-based route enforcement with fallback to user claims
        int? roleId = context.Session.GetInt32("RoleId");
        if (!roleId.HasValue && context.User?.Identity?.IsAuthenticated == true)
        {
            var roleIdClaim = context.User.FindFirst("RoleId")?.Value;
            if (int.TryParse(roleIdClaim, out int parsedRid))
            {
                roleId = parsedRid;
                context.Session.SetInt32("RoleId", parsedRid);
            }
            else
            {
                var roleName = context.User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value;
                if (!string.IsNullOrEmpty(roleName))
                {
                    roleId = roleName switch
                    {
                        "Super Admin" or "SuperAdmin" => 1,
                        "Staff" or "Staff / HR" => 3,
                        "Supplier" or "Supplier Partner" => 4,
                        _ => 2
                    };
                    context.Session.SetInt32("RoleId", roleId.Value);
                }
            }
        }

        if (!roleId.HasValue)
        {
            context.Response.Redirect("/Account/Login");
            return;
        }

        if (roleId.HasValue)
        {
            if (path.StartsWith("/SuperAdmin", StringComparison.OrdinalIgnoreCase) && roleId.Value != 1)
            {
                context.Response.Redirect(roleId.Value switch
                {
                    4 => "/Supplier/MyCompliance",
                    3 => "/Staff/Suppliers",
                    _ => "/Admin/Dashboard"
                });
                return;
            }

            if (path.StartsWith("/Admin", StringComparison.OrdinalIgnoreCase) && roleId.Value != 1 && roleId.Value != 2)
            {
                context.Response.Redirect(roleId.Value switch
                {
                    4 => "/Supplier/MyCompliance",
                    3 => "/Staff/Suppliers",
                    _ => "/Account/Login"
                });
                return;
            }

            if (path.StartsWith("/Staff", StringComparison.OrdinalIgnoreCase) && roleId.Value != 1 && roleId.Value != 3)
            {
                context.Response.Redirect(roleId.Value switch
                {
                    4 => "/Supplier/MyCompliance",
                    _ => "/Admin/Dashboard"
                });
                return;
            }

            if (path.StartsWith("/Supplier", StringComparison.OrdinalIgnoreCase) && roleId.Value != 4)
            {
                context.Response.Redirect(roleId.Value switch
                {
                    1 => "/SuperAdmin/Overview",
                    3 => "/Staff/Suppliers",
                    _ => "/Admin/Dashboard"
                });
                return;
            }
        }
    }

    await next();
});

app.MapRazorPages();

// Run DB Seeder on startup
try
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<DoComplyContext>();
    await DbSeeder.SeedAsync(db);

    if (args.Contains("--seed-only") || args.Contains("--seed"))
    {
        Console.WriteLine("[DoComply] Database seeding completed. Exiting application.");
        return;
    }
}
catch (Exception ex)
{
    Console.WriteLine($"[DbSeeder] Warning: {ex.Message}");
    if (ex.InnerException != null)
    {
        Console.WriteLine($"[DbSeeder] Inner Exception: {ex.InnerException.Message}");
    }
    if (args.Contains("--seed-only") || args.Contains("--seed"))
    {
        return;
    }
}

app.Run();
