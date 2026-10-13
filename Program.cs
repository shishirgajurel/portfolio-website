using System.Text;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using PortfolioSite.Data;
using PortfolioSite.Services;

var builder = WebApplication.CreateBuilder(args);

// Lets the site run as a Windows service on the laptop server (no effect elsewhere).
builder.Host.UseWindowsService();

// Optional file with the admin login on the server, kept outside the app folder.
if (builder.Configuration["SecretsFile"] is { Length: > 0 } secretsFile)
    builder.Configuration.AddJsonFile(secretsFile, optional: true, reloadOnChange: false);

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("Default") ?? "Data Source=portfolio.db"));
builder.Services.AddScoped<ImageStorage>();

// Keep the login-cookie keys on disk so the admin stays logged in after a restart.
builder.Services.AddDataProtection()
    .PersistKeysToFileSystem(new DirectoryInfo(builder.Configuration["DataProtection:KeysPath"] ?? "keys"));

// The public site runs behind Cloudflare / a reverse proxy that handles HTTPS.
// Trust its headers so the app knows the real scheme (https) and visitor IP.
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
});

builder.Services.AddRazorPages(options =>
{
    // Everything under /admin needs a login, except the login page itself.
    options.Conventions.AuthorizeFolder("/Admin");
    options.Conventions.AllowAnonymousToPage("/Admin/Login");
});

builder.Services
    .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/admin/login";
        options.AccessDeniedPath = "/admin/login";
        options.Cookie.Name = "portfolio_admin";
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Strict;
        options.Cookie.SecurePolicy = builder.Environment.IsDevelopment()
            ? CookieSecurePolicy.SameAsRequest
            : CookieSecurePolicy.Always;
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
        options.SlidingExpiration = true;
    });
builder.Services.AddAuthorization();

// Slows down password guessing: max 10 requests per minute per IP on the login page.
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy("login", context => RateLimitPartition.GetFixedWindowLimiter(
        context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
        _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = 10,
            Window = TimeSpan.FromMinutes(1),
            QueueLimit = 0
        }));
});

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    SeedData.Initialize(scope.ServiceProvider.GetRequiredService<AppDbContext>());
}

app.UseForwardedHeaders();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseStatusCodePagesWithReExecute("/Error", "?code={0}");
app.UseHttpsRedirection();

app.Use(async (context, next) =>
{
    var headers = context.Response.Headers;
    headers["X-Content-Type-Options"] = "nosniff";
    headers["X-Frame-Options"] = "DENY";
    headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
    headers["Permissions-Policy"] = "camera=(), microphone=(), geolocation=()";
    headers["Content-Security-Policy"] =
        "default-src 'self'; img-src 'self' data:; style-src 'self'; script-src 'self'; " +
        "base-uri 'self'; form-action 'self'; frame-ancestors 'none'";

    // Keep the admin area out of search engines without advertising it in robots.txt.
    if (context.Request.Path.StartsWithSegments("/admin"))
        headers["X-Robots-Tag"] = "noindex, nofollow";

    await next();
});

app.UseStaticFiles();
app.UseRouting();
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/robots.txt", (HttpContext ctx, IConfiguration config) =>
    Results.Text(
        $"User-agent: *\nAllow: /\n\nSitemap: {SiteUrl.BaseUrl(ctx.Request, config)}/sitemap.xml\n",
        "text/plain"));

app.MapGet("/sitemap.xml", async (HttpContext ctx, AppDbContext db, IConfiguration config) =>
{
    var baseUrl = SiteUrl.BaseUrl(ctx.Request, config);
    var projects = await db.Projects.AsNoTracking().Where(p => p.Published).ToListAsync();

    var sb = new StringBuilder();
    sb.AppendLine("<?xml version=\"1.0\" encoding=\"UTF-8\"?>");
    sb.AppendLine("<urlset xmlns=\"http://www.sitemaps.org/schemas/sitemap/0.9\">");
    sb.AppendLine($"  <url><loc>{baseUrl}/</loc></url>");
    foreach (var p in projects)
        sb.AppendLine($"  <url><loc>{baseUrl}/projects/{p.Slug}</loc><lastmod>{p.Updated:yyyy-MM-dd}</lastmod></url>");
    sb.AppendLine("</urlset>");

    return Results.Content(sb.ToString(), "application/xml");
});

app.MapRazorPages();

app.Run();
