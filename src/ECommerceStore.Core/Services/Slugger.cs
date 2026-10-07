using System.Text;

namespace ECommerceStore.Core.Services;

/// <summary>Turns a product name into a URL-friendly slug ("Waterproof Scrubs" becomes "waterproof-scrubs").</summary>
public static class Slugger
{
    public static string Slugify(string? text)
    {
        var builder = new StringBuilder();
        var lastWasDash = true; // avoids a leading dash

        foreach (var ch in (text ?? string.Empty).Trim().ToLowerInvariant())
        {
            if (ch is >= 'a' and <= 'z' or >= '0' and <= '9')
            {
                builder.Append(ch);
                lastWasDash = false;
            }
            else if (!lastWasDash)
            {
                builder.Append('-');
                lastWasDash = true;
            }
        }

        var slug = builder.ToString().Trim('-');
        if (slug.Length > 100)
        {
            slug = slug[..100].Trim('-');
        }

        return slug.Length == 0 ? "product" : slug;
    }

    /// <summary>Slugifies the name and adds -2, -3... until <paramref name="isTaken"/> says the slug is free.</summary>
    public static async Task<string> MakeUniqueAsync(string? name, Func<string, Task<bool>> isTaken)
    {
        var baseSlug = Slugify(name);
        var candidate = baseSlug;
        var counter = 2;

        while (await isTaken(candidate))
        {
            candidate = $"{baseSlug}-{counter++}";
        }

        return candidate;
    }
}
