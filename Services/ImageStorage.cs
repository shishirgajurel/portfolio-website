namespace PortfolioSite.Services;

/// <summary>Validates and stores uploaded images in wwwroot/uploads.</summary>
public class ImageStorage(IWebHostEnvironment env)
{
    private const long MaxBytes = 5 * 1024 * 1024;

    public async Task<(string? Url, string? Error)> SaveAsync(IFormFile file)
    {
        if (file.Length == 0 || file.Length > MaxBytes)
            return (null, "The image must be smaller than 5 MB.");

        await using var input = file.OpenReadStream();
        var header = new byte[12];
        var read = await input.ReadAsync(header);

        // Decide the type from the file's real bytes, never from the file name.
        string? ext = null;
        if (read >= 3 && header[0] == 0xFF && header[1] == 0xD8 && header[2] == 0xFF) ext = ".jpg";
        else if (read >= 8 && header[0] == 0x89 && header[1] == 0x50 && header[2] == 0x4E && header[3] == 0x47) ext = ".png";
        else if (read >= 12 && header[0] == 'R' && header[1] == 'I' && header[2] == 'F' && header[3] == 'F'
                 && header[8] == 'W' && header[9] == 'E' && header[10] == 'B' && header[11] == 'P') ext = ".webp";

        if (ext is null) return (null, "Only JPG, PNG or WebP images are allowed.");

        var dir = Path.Combine(env.WebRootPath, "uploads");
        Directory.CreateDirectory(dir);

        var name = Guid.NewGuid().ToString("N") + ext;
        input.Position = 0;
        await using var output = File.Create(Path.Combine(dir, name));
        await input.CopyToAsync(output);

        return ("/uploads/" + name, null);
    }

    public void Delete(string? url)
    {
        if (string.IsNullOrEmpty(url) || !url.StartsWith("/uploads/")) return;
        var full = Path.Combine(env.WebRootPath, "uploads", Path.GetFileName(url));
        if (File.Exists(full)) File.Delete(full);
    }
}
