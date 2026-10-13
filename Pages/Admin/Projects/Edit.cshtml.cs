using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using PortfolioSite.Data;
using PortfolioSite.Models;
using PortfolioSite.Services;

namespace PortfolioSite.Pages.Admin.Projects;

public class EditModel(AppDbContext db, ImageStorage images) : PageModel
{
    [BindProperty] public Project Item { get; set; } = new();
    [BindProperty] public IFormFile? Image { get; set; }
    [BindProperty] public bool RemoveImage { get; set; }

    public async Task<IActionResult> OnGetAsync(int? id)
    {
        if (id is null) return Page();

        var project = await db.Projects.AsNoTracking().FirstOrDefaultAsync(p => p.Id == id);
        if (project is null) return NotFound();

        Item = project;
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(int? id)
    {
        Project entity;
        if (id is null)
        {
            entity = new Project();
        }
        else
        {
            var existing = await db.Projects.FirstOrDefaultAsync(p => p.Id == id);
            if (existing is null) return NotFound();
            entity = existing;
        }

        if (!ModelState.IsValid)
        {
            Item.ImagePath = entity.ImagePath;
            return Page();
        }

        if (Image is { Length: > 0 })
        {
            var (url, error) = await images.SaveAsync(Image);
            if (error is not null)
            {
                ModelState.AddModelError("Image", error);
                Item.ImagePath = entity.ImagePath;
                return Page();
            }
            images.Delete(entity.ImagePath);
            entity.ImagePath = url;
        }
        else if (RemoveImage)
        {
            images.Delete(entity.ImagePath);
            entity.ImagePath = null;
        }

        entity.Title = Item.Title;
        entity.Summary = Item.Summary;
        entity.Tags = Item.Tags;
        entity.Link = Item.Link;
        entity.Details = Item.Details;
        entity.SortOrder = Item.SortOrder;
        entity.Published = Item.Published;
        entity.Updated = DateTime.UtcNow;

        // The web address (slug) is created once and then kept stable so links never break.
        if (string.IsNullOrEmpty(entity.Slug))
        {
            var baseSlug = SlugHelper.Create(entity.Title);
            var slug = baseSlug;
            var n = 2;
            while (await db.Projects.AnyAsync(p => p.Slug == slug))
                slug = $"{baseSlug}-{n++}";
            entity.Slug = slug;
        }

        if (id is null) db.Projects.Add(entity);
        await db.SaveChangesAsync();

        return RedirectToPage("Index");
    }
}
