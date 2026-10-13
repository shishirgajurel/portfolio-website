using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PortfolioSite.Models;

public class SiteSettings
{
    public int Id { get; set; }

    [Required, StringLength(100)] public string FullName { get; set; } = "";
    [Required, StringLength(160)] public string Headline { get; set; } = "";
    [Required, StringLength(600)] public string Intro { get; set; } = "";
    [StringLength(3000)] public string? About { get; set; }
    [StringLength(600)] public string? Skills { get; set; }
    [Required, EmailAddress, StringLength(200)] public string Email { get; set; } = "";
    [StringLength(200)] public string? Address { get; set; }
    [StringLength(300)] public string? Availability { get; set; }
    [Url, StringLength(300)] public string? GitHubUrl { get; set; }
    [Url, StringLength(300)] public string? LinkedInUrl { get; set; }
    public string? PhotoPath { get; set; }

    [NotMapped]
    public IEnumerable<string> SkillList =>
        (Skills ?? "").Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
}
