using System.Text;
using System.Text.RegularExpressions;

namespace WearMate.Shared.Helpers;

public static class SlugHelper
{
    /// <summary>
    /// Create a URL-friendly slug from text. Falls back to provided fallback or a GUID when empty.
    /// </summary>
    public static string Slugify(string? text, string? fallback = null)
    {
        var source = string.IsNullOrWhiteSpace(text) ? fallback : text;
        if (string.IsNullOrWhiteSpace(source))
            source = Guid.NewGuid().ToString();

        var normalized = source.Normalize(NormalizationForm.FormKD);
        var sb = new StringBuilder();
        foreach (var ch in normalized)
        {
            if (char.IsLetterOrDigit(ch) || ch == ' ' || ch == '-') sb.Append(ch);
        }

        var slug = sb.ToString().ToLowerInvariant().Trim();
        slug = Regex.Replace(slug, "\\s+", "-");
        slug = Regex.Replace(slug, "-+", "-");

        if (string.IsNullOrWhiteSpace(slug))
            slug = Guid.NewGuid().ToString();

        return slug;
    }
}
