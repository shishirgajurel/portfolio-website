using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.RateLimiting;

namespace PortfolioSite.Pages.Admin;

[EnableRateLimiting("login")]
public class LoginModel(IConfiguration config) : PageModel
{
    [BindProperty] public string Email { get; set; } = "";
    [BindProperty] public string Password { get; set; } = "";
    [BindProperty(SupportsGet = true)] public string? ReturnUrl { get; set; }
    public string? Error { get; private set; }

    public IActionResult OnGet()
    {
        if (User.Identity?.IsAuthenticated == true) return Redirect("/admin");
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var adminEmail = config["Admin:Email"];
        var adminPassword = config["Admin:Password"];

        if (string.IsNullOrWhiteSpace(adminEmail) || string.IsNullOrWhiteSpace(adminPassword))
        {
            Error = "Admin credentials are not configured on the server.";
            return Page();
        }

        var ok = SafeEquals(Email.Trim().ToLowerInvariant(), adminEmail.Trim().ToLowerInvariant())
                 & SafeEquals(Password, adminPassword);

        if (!ok)
        {
            await Task.Delay(500); // makes guessing slower
            Error = "Wrong email or password.";
            return Page();
        }

        var identity = new ClaimsIdentity(
            new[] { new Claim(ClaimTypes.Name, adminEmail), new Claim(ClaimTypes.Role, "Admin") },
            CookieAuthenticationDefaults.AuthenticationScheme);

        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(identity));

        return LocalRedirect(Url.IsLocalUrl(ReturnUrl) ? ReturnUrl! : "/admin");
    }

    // Compares two strings in constant time so timing can't leak information.
    private static bool SafeEquals(string a, string b) =>
        CryptographicOperations.FixedTimeEquals(
            SHA256.HashData(Encoding.UTF8.GetBytes(a)),
            SHA256.HashData(Encoding.UTF8.GetBytes(b)));
}
