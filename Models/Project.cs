using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PortfolioSite.Models;

public class Project
{
    public int Id { get; set; }

    [Required, StringLength(120)] public string Title { get; set; } = "";
    [StringLength(120)] public string? Slug { get; set; }
    [Required, StringLength(300)] public string Summary { get; set; } = "";
    [StringLength(300)] public string? Tags { get; set; }
    [Url, StringLength(300)] public string? Link { get; set; }
    public string? ImagePath { get; set; }
    [StringLength(8000)] public string? Details { get; set; }
    public int SortOrder { get; set; }
    public bool Published { get; set; } = true;
    public DateTime Updated { get; set; } = DateTime.UtcNow;

    [NotMapped]
    public IEnumerable<string> TagList =>
        (Tags ?? "").Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
}
