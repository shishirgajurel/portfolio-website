namespace PortfolioSite.Services;

public static class SiteUrl
{
    /// <summary>
    /// The site's public address, e.g. "https://www.shishirgajurel.com.np".
    /// Uses Site:BaseUrl when set (needed for the static GitHub export, which runs on localhost),
    /// otherwise the address of the current request.
    /// </summary>
    public static string BaseUrl(HttpRequest request, IConfiguration config)
    {
        var configured = config["Site:BaseUrl"];
        return string.IsNullOrWhiteSpace(configured)
            ? $"{request.Scheme}://{request.Host}"
            : configured.TrimEnd('/');
    }
}
