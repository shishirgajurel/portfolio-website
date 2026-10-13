using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Caching.Memory;

namespace PortfolioSite.Services;

public record GitHubRepo(
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("description")] string? Description,
    [property: JsonPropertyName("html_url")] string HtmlUrl,
    [property: JsonPropertyName("language")] string? Language,
    [property: JsonPropertyName("topics")] string[]? Topics,
    [property: JsonPropertyName("pushed_at")] DateTime PushedAt,
    [property: JsonPropertyName("archived")] bool Archived,
    [property: JsonPropertyName("fork")] bool Fork,
    [property: JsonPropertyName("stargazers_count")] int Stars);

/// <summary>
/// Loads the public repositories of the GitHub user in Site:GitHubUser.
/// Forks, the profile README repo and the GitHub Pages repo are left out.
/// Results are cached for 30 minutes; if GitHub can't be reached the list is simply empty.
/// </summary>
public class GitHubRepos(HttpClient http, IMemoryCache cache, IConfiguration config, ILogger<GitHubRepos> log)
{
    public string User => config["Site:GitHubUser"] ?? "";

    public async Task<IReadOnlyList<GitHubRepo>> GetAsync()
    {
        if (string.IsNullOrWhiteSpace(User)) return [];

        return await cache.GetOrCreateAsync($"github-repos:{User}", async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(30);
            try
            {
                var json = await http.GetStringAsync($"users/{Uri.EscapeDataString(User)}/repos?per_page=100&sort=pushed");
                var repos = JsonSerializer.Deserialize<List<GitHubRepo>>(json) ?? [];
                return Filter(repos, User);
            }
            catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or JsonException)
            {
                log.LogWarning(ex, "Could not load GitHub repositories for {User}", User);
                entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(2);
                return (IReadOnlyList<GitHubRepo>)[];
            }
        }) ?? [];
    }

    // Same rules as site.js, so the server snapshot and the live list match.
    private static IReadOnlyList<GitHubRepo> Filter(IEnumerable<GitHubRepo> repos, string user) =>
        repos.Where(r => !r.Fork
                         && !r.Name.Equals(user, StringComparison.OrdinalIgnoreCase)
                         && !r.Name.Equals($"{user}.github.io", StringComparison.OrdinalIgnoreCase))
             .OrderBy(r => r.Archived)
             .ThenByDescending(r => r.PushedAt)
             .ToList();
}
