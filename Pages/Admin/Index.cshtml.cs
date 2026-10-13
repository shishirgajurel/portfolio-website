using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using PortfolioSite.Data;
using PortfolioSite.Models;
using PortfolioSite.Services;

namespace PortfolioSite.Pages.Admin;

public class IndexModel(AppDbContext db, ImageStorage images) : PageModel
{
    [BindProperty] public SiteSettings Site { get; set; } = new();
    [BindProperty] public IFormFile? Photo { get; set; }
    [BindProperty] public bool RemovePhoto { get; set; }
    [TempData] public string? Saved { get; set; }

    public async Task OnGetAsync()
    {
        Site = await db.Settings.AsNoTracking().FirstAsync();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var current = await db.Settings.FirstAsync();

        if (!ModelState.IsValid)
        {
            Site.PhotoPath = current.PhotoPath;
            return Page();
        }

        if (Photo is { Length: > 0 })
        {
            var (url, error) = await images.SaveAsync(Photo);
            if (error is not null)
            {
                ModelState.AddModelError("Photo", error);
                Site.PhotoPath = current.PhotoPath;
                return Page();
            }
            images.Delete(current.PhotoPath);
            current.PhotoPath = url;
        }
        else if (RemovePhoto)
        {
            images.Delete(current.PhotoPath);
            current.PhotoPath = null;
        }

        // Copy field by field so nothing unexpected can be overwritten.
        current.FullName = Site.FullName;
        current.Headline = Site.Headline;
        current.Intro = Site.Intro;
        current.About = Site.About;
        current.Skills = Site.Skills;
        current.Email = Site.Email;
        current.Address = Site.Address;
        current.Availability = Site.Availability;
        current.GitHubUrl = Site.GitHubUrl;
        current.LinkedInUrl = Site.LinkedInUrl;

        await db.SaveChangesAsync();
        Saved = "Saved. Your changes are live.";
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostLogoutAsync()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return Redirect("/");
    }
}
