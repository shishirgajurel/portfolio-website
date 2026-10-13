using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace PortfolioSite.Pages;

[IgnoreAntiforgeryToken]
public class ErrorModel : PageModel
{
    [BindProperty(SupportsGet = true)] public int? Code { get; set; }

    public string Heading => Code == 404 ? "Page not found" : "Something went wrong";
    public string Message => Code == 404
        ? "The page you're looking for doesn't exist or has moved."
        : "An unexpected error occurred. Please try again in a moment.";
}
