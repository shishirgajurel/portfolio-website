using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using PortfolioSite.Data;
using PortfolioSite.Models;
using PortfolioSite.Services;

namespace PortfolioSite.Pages;

public class IndexModel(AppDbContext db, GitHubRepos gitHub) : PageModel
{
    public SiteSettings Site { get; private set; } = null!;
    public List<Project> Projects { get; private set; } = [];
    public IReadOnlyList<GitHubRepo> Repos { get; private set; } = [];
    public string GitHubUser => gitHub.User;

    /// <summary>GitHub URL (lower-case, no trailing slash) → project page, for repos that have a write-up.</summary>
    public Dictionary<string, string> Featured { get; private set; } = [];

    public async Task OnGetAsync()
    {
        Site = await db.Settings.AsNoTracking().FirstAsync();
        Projects = await db.Projects.AsNoTracking()
            .Where(p => p.Published)
            .OrderBy(p => p.SortOrder).ThenBy(p => p.Id)
            .ToListAsync();

        Featured = Projects
            .Where(p => !string.IsNullOrEmpty(p.Link) && !string.IsNullOrEmpty(p.Slug))
            .GroupBy(p => NormalizeUrl(p.Link!))
            .ToDictionary(g => g.Key, g => $"/projects/{g.First().Slug}");

        // Repos with a write-up first; GitHubRepos already sorts the rest (active by date, archived last).
        Repos = (await gitHub.GetAsync())
            .OrderByDescending(r => Featured.ContainsKey(NormalizeUrl(r.HtmlUrl)))
            .ToList();
    }

    public static string NormalizeUrl(string url) => url.Trim().TrimEnd('/').ToLowerInvariant();
}
