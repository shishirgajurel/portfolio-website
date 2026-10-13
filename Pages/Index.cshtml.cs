using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using PortfolioSite.Data;
using PortfolioSite.Models;

namespace PortfolioSite.Pages;

public class IndexModel(AppDbContext db) : PageModel
{
    public SiteSettings Site { get; private set; } = null!;
    public List<Project> Projects { get; private set; } = [];

    public async Task OnGetAsync()
    {
        Site = await db.Settings.AsNoTracking().FirstAsync();
        Projects = await db.Projects.AsNoTracking()
            .Where(p => p.Published)
            .OrderBy(p => p.SortOrder).ThenBy(p => p.Id)
            .ToListAsync();
    }
}
