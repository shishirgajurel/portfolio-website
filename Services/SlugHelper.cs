using System.Globalization;
using System.Text;

namespace PortfolioSite.Services;

public static class SlugHelper
{
    public static string Create(string title)
    {
        var sb = new StringBuilder();
        foreach (var ch in title.ToLowerInvariant().Normalize(NormalizationForm.FormD))
        {
            if (CharUnicodeInfo.GetUnicodeCategory(ch) == UnicodeCategory.NonSpacingMark) continue;
            if (char.IsAsciiLetterOrDigit(ch)) sb.Append(ch);
            else if (sb.Length > 0 && sb[^1] != '-') sb.Append('-');
        }
        var slug = sb.ToString().Trim('-');
        return slug.Length == 0 ? "project" : slug;
    }
}
