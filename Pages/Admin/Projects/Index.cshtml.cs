using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using PortfolioSite.Data;
using PortfolioSite.Models;
using PortfolioSite.Services;

namespace PortfolioSite.Pages.Admin.Projects;

public class IndexModel(AppDbContext db, ImageStorage images) : PageModel
{
    public List<Project> Items { get; private set; } = [];

    public async Task OnGetAsync()
    {
        Items = await db.Projects.AsNoTracking().OrderBy(p => p.SortOrder).ThenBy(p => p.Id).ToListAsync();
    }

    public async Task<IActionResult> OnPostDeleteAsync(int id)
    {
        var project = await db.Projects.FindAsync(id);
        if (project is not null)
        {
            images.Delete(project.ImagePath);
            db.Projects.Remove(project);
            await db.SaveChangesAsync();
        }
        return RedirectToPage();
    }
}
