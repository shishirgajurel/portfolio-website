using System.Net;
using System.Text;

namespace PortfolioSite.Services;

/// <summary>
/// Turns the simple text format used in the admin panel into safe HTML.
/// "## Heading" becomes a heading, "- item" becomes a bullet, other lines become paragraphs.
/// Everything is HTML-encoded, so nothing typed in the admin can inject markup.
/// </summary>
public static class ContentRenderer
{
    public static string ToHtml(string? text)
    {
        if (string.IsNullOrWhiteSpace(text)) return "";

        var sb = new StringBuilder();
        var inList = false;

        foreach (var raw in text.Replace("\r\n", "\n").Split('\n'))
        {
            var line = raw.Trim();
            if (line.Length == 0) { CloseList(); continue; }

            if (line.StartsWith("## "))
            {
                CloseList();
                sb.Append("<h3>").Append(WebUtility.HtmlEncode(line[3..])).Append("</h3>");
            }
            else if (line.StartsWith("- "))
            {
                if (!inList) { sb.Append("<ul>"); inList = true; }
                sb.Append("<li>").Append(WebUtility.HtmlEncode(line[2..])).Append("</li>");
            }
            else
            {
                CloseList();
                sb.Append("<p>").Append(WebUtility.HtmlEncode(line)).Append("</p>");
            }
        }

        CloseList();
        return sb.ToString();

        void CloseList()
        {
            if (inList) { sb.Append("</ul>"); inList = false; }
        }
    }
}
