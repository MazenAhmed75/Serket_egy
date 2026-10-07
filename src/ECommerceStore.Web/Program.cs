using System.Globalization;
using System.Threading.RateLimiting;
using ECommerceStore.Core.Interfaces;
using ECommerceStore.Core.Services;
using ECommerceStore.Web.Services;
using ECommerceStore.Infrastructure.Data;
using ECommerceStore.Infrastructure.Repositories;
using ECommerceStore.Infrastructure.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

// Helper for setting the admin password: run
//   dotnet run --project src/ECommerceStore.Web -- hash-password "YourNewPassword"
// It prints the value to paste into Admin__PasswordHash (in .env or appsettings.json), then exits.
if (args.Length == 2 && args[0] == "hash-password")
{
    Console.WriteLine(Pbkdf2PasswordHasher.Hash(args[1]));
    return;
}

// Local-dev convenience only: load ".env" files into real process environment variables *before*
// WebApplication.CreateBuilder reads configuration (environment variables override appsettings.json).
// Every ".env" from the current folder up through its parents is loaded. They are applied
// farthest-first so the one closest to where the app runs wins when the same name appears twice.
// Production hosts don't ship a .env file; they set these same variable names directly instead.
var envFiles = new List<string>();
var searchDir = new DirectoryInfo(Directory.GetCurrentDirectory());
while (searchDir != null)
{
    var candidate = Path.Combine(searchDir.FullName, ".env");
    if (File.Exists(candidate))
    {
        envFiles.Add(candidate);
    }
    searchDir = searchDir.Parent;
}

envFiles.Reverse();
foreach (var envFile in envFiles)
{
    DotNetEnv.Env.Load(envFile);
}

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
if (string.IsNullOrWhiteSpace(connectionString))
{
    throw new InvalidOperationException(
        "Connection string 'DefaultConnection' was not found or is empty in configuration.");
}

// Fail fast on a production host that still uses the admin login shipped in appsettings.json.
if (!builder.Environment.IsDevelopment())
{
    var shippedDefaults = new ConfigurationBuilder()
        .SetBasePath(builder.Environment.ContentRootPath)
        .AddJsonFile("appsettings.json", optional: true)
        .Build();

    var adminHash = builder.Configuration["Admin:PasswordHash"];
    if (string.IsNullOrWhiteSpace(adminHash) || adminHash == shippedDefaults["Admin:PasswordHash"])
    {
        throw new InvalidOperationException(
            "Admin:PasswordHash must be set to your own password hash (environment variable Admin__PasswordHash), " +
            "not the value shipped in appsettings.json. Create one with: dotnet run --project src/ECommerceStore.Web -- hash-password \"YourPassword\"");
    }
}

// A pool of reusable DbContext instances is cheaper per request than creating a new one each time.
builder.Services.AddDbContextPool<AppDbContext>(options =>
    options.UseNpgsql(connectionString));

// Cookies and anti-forgery tokens are protected with keys. Without a persistent key folder a restart or a second
// server generates new keys, which logs everyone out and breaks confirmation links. Set DataProtection:KeysPath
// (environment variable DataProtection__KeysPath) to a folder that survives restarts in production.
var dataProtection = builder.Services.AddDataProtection().SetApplicationName("Serket");
var keysPath = builder.Configuration["DataProtection:KeysPath"];
if (!string.IsNullOrWhiteSpace(keysPath))
{
    dataProtection.PersistKeysToFileSystem(new DirectoryInfo(keysPath));
}

// Cookies only travel over HTTPS (except on a developer's machine), are hidden from scripts and are not sent on
// cross-site requests that change something.
var cookieSecurity = builder.Environment.IsDevelopment() ? CookieSecurePolicy.SameAsRequest : CookieSecurePolicy.Always;
builder.Services.AddAntiforgery(options => options.Cookie.SecurePolicy = cookieSecurity);

// Slow down floods and password guessing. The limits are per client address; static files are not counted.
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.OnRejected = (context, _) =>
    {
        if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
        {
            context.HttpContext.Response.Headers.RetryAfter = ((int)retryAfter.TotalSeconds).ToString(CultureInfo.InvariantCulture);
        }

        return ValueTask.CompletedTask;
    };

    FixedWindowRateLimiterOptions PerMinute(int limit) => new() { PermitLimit = limit, Window = TimeSpan.FromMinutes(1), QueueLimit = 0 };
    static string ClientKey(HttpContext context) => context.Connection.RemoteIpAddress?.ToString() ?? "unknown";

    // Everything: generous, only stops obvious floods.
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(
        context => RateLimitPartition.GetFixedWindowLimiter(ClientKey(context), _ => PerMinute(600)));

    // Log in / sign up (password guessing). Several customers can share one mobile-network address, so this is not too tight.
    options.AddPolicy("auth", context => RateLimitPartition.GetFixedWindowLimiter(ClientKey(context), _ => PerMinute(20)));

    // Pages that write data (checkout, promo codes, reviews, reminders).
    options.AddPolicy("shopping", context => RateLimitPartition.GetFixedWindowLimiter(ClientKey(context), _ => PerMinute(60)));
});

// A single scoped UnitOfWork exposes all repositories (Products, Customers, Orders,
// OrderItems, PaymentReceipts) so every use case commits through one DbContext/transaction.
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
builder.Services.AddScoped<PromoCodeService>();
builder.Services.AddScoped<ReviewService>();
builder.Services.AddScoped<StockReminderService>();

// Product photos are public (wwwroot/uploads). Payment receipts are private: they live outside the web root and are
// only shown through the admin-only receipt page. Storage:PrivatePath can point somewhere else (a mounted volume).
var privateFilesPath = builder.Configuration["Storage:PrivatePath"]
    ?? Path.Combine(builder.Environment.ContentRootPath, "App_Data", "private");
builder.Services.AddSingleton<IFileStorageService>(
    _ => new LocalFileStorageService(builder.Environment.WebRootPath, privateFilesPath));

builder.Services.AddSingleton<OrderConfirmationLinks>();
builder.Services.AddScoped<IEmailNotifier, SmtpEmailNotifier>();

// Cookie auth protects the whole /Admin area (see the AuthorizeAreaFolder convention below).
// There's no Users table — the single owner account lives in appsettings' Admin:* keys,
// hashed with Pbkdf2PasswordHasher.
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.Cookie.Name = ".Serket.Admin";
        options.Cookie.HttpOnly = true;
        options.Cookie.SecurePolicy = cookieSecurity;
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.LoginPath = "/Admin/Login";
        options.AccessDeniedPath = "/Admin/Login";
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
        options.SlidingExpiration = true;
    })
    // Separate cookie for shoppers who create an account (sign up / log in on the storefront).
    .AddCookie(CustomerAuth.Scheme, options =>
    {
        options.Cookie.Name = ".Serket.Customer";
        options.Cookie.HttpOnly = true;
        options.Cookie.SecurePolicy = cookieSecurity;
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.LoginPath = "/Account/Login";
        options.AccessDeniedPath = "/Account/Login";
        options.ExpireTimeSpan = TimeSpan.FromDays(30);
        options.SlidingExpiration = true;
    });

builder.Services.AddAuthorization();

builder.Services.AddRazorPages(options =>
{
    options.Conventions.AuthorizeAreaFolder("Admin", "/");
    options.Conventions.AllowAnonymousToAreaPage("Admin", "/Login");
    options.Conventions.AllowAnonymousToAreaPage("Admin", "/Logout");
});

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();

    if (app.Configuration["AllowedHosts"] is null or "*")
    {
        app.Logger.LogWarning("AllowedHosts is '*'. In production set it to your domain (environment variable AllowedHosts).");
    }
}

// Friendly pages for 404, 429 and other empty error responses.
app.UseStatusCodePagesWithReExecute("/Error/{0}");

app.UseHttpsRedirection();

// Security headers go first so they also cover static files (uploaded images included).
// script-src has no 'unsafe-inline': every script is a file served from this site.
app.Use(async (context, next) =>
{
    var headers = context.Response.Headers;
    headers.Append("X-Content-Type-Options", "nosniff");
    headers.Append("X-Frame-Options", "DENY");
    headers.Append("Referrer-Policy", "strict-origin-when-cross-origin");
    headers.Append("Permissions-Policy", "geolocation=(self), camera=(), microphone=(), payment=()");
    headers.Append(
        "Content-Security-Policy",
        "default-src 'self'; " +
        "script-src 'self'; " +
        "style-src 'self' 'unsafe-inline' https://fonts.googleapis.com; " +
        "font-src 'self' https://fonts.gstatic.com; " +
        "img-src 'self' data: blob: https://*.tile.openstreetmap.org https://tile.openstreetmap.org; " +
        "connect-src 'self' https://nominatim.openstreetmap.org; " +
        "object-src 'none'; base-uri 'self'; form-action 'self'; frame-ancestors 'none'");
    await next();
});

// Browsers keep stylesheets, scripts and images for a while so repeat visitors (and the server) do less work.
// Files requested with a ?v= version stamp can safely be kept for a year because the stamp changes with the file.
app.UseStaticFiles(new StaticFileOptions
{
    OnPrepareResponse = context =>
    {
        var request = context.Context.Request;
        var seconds = request.Query.ContainsKey("v") ? (int)TimeSpan.FromDays(365).TotalSeconds : (int)TimeSpan.FromDays(1).TotalSeconds;
        context.Context.Response.Headers.CacheControl = $"public,max-age={seconds}";
    }
});

app.UseRouting();

app.UseRateLimiter();

app.UseAuthentication();
app.UseAuthorization();
app.Use(CustomerAuth.LoadCustomerAsync);

app.MapRazorPages();

// Is the process alive? (no database): for "restart it if this stops answering".
app.MapGet("/health/live", () => Results.Ok(new { status = "alive" })).DisableRateLimiting();

// Can it serve customers? (checks the database): for uptime monitors and load balancers.
app.MapGet("/health", async (AppDbContext db) =>
{
    try
    {
        return await db.Database.CanConnectAsync()
            ? Results.Ok(new { status = "healthy", database = "connected" })
            : Results.Problem("Database is unreachable.", statusCode: StatusCodes.Status503ServiceUnavailable);
    }
    catch (Exception)
    {
        return Results.Problem("Database check failed.", statusCode: StatusCodes.Status503ServiceUnavailable);
    }
}).DisableRateLimiting();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await DbSeeder.SeedAsync(db);
}

app.Run();
