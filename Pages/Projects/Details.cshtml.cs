using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using PortfolioSite.Data;
using PortfolioSite.Models;

namespace PortfolioSite.Pages.Projects;

public class DetailsModel(AppDbContext db) : PageModel
{
    public Project Project { get; private set; } = null!;

    public async Task<IActionResult> OnGetAsync(string slug)
    {
        var project = await db.Projects.AsNoTracking()
            .FirstOrDefaultAsync(p => p.Slug == slug && p.Published);
        if (project is null) return NotFound();

        Project = project;
        return Page();
    }
}
