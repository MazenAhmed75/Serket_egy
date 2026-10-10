using System.Text.RegularExpressions;

namespace ECommerceStore.Core.Services;

/// <summary>
/// The rules every file-storage implementation shares: generated file names, a safe image extension, and the exact
/// shape of the references we store in the database. Anything that doesn't match is never opened, deleted or served.
/// </summary>
public static class StoredFileNames
{
    public const string PrivatePrefix = "private:";

    private static readonly HashSet<string> SafeExtensions = new(StringComparer.OrdinalIgnoreCase) { ".jpg", ".jpeg", ".png", ".webp" };

    // "private:receipts/<32 hex characters>.<jpg|jpeg|png|webp>"
    private static readonly Regex PrivatePattern =
        new(@"^private:(?<folder>[a-z]{1,30})/(?<file>[0-9a-f]{32}\.(jpg|jpeg|png|webp))$", RegexOptions.Compiled | RegexOptions.CultureInvariant);

    // "<folder>/<32 hex characters>.<ext>" (the path of a public file inside its bucket or under /uploads)
    private static readonly Regex PublicObjectPattern =
        new(@"^(?<folder>[a-z]{1,30})/(?<file>[0-9a-f]{32}\.(jpg|jpeg|png|webp))$", RegexOptions.Compiled | RegexOptions.CultureInvariant);

    /// <summary>A fresh random name keeping only a safe image extension (anything else becomes ".jpg").</summary>
    public static string NewFileName(string originalFileName)
    {
        var extension = Path.GetExtension(originalFileName).ToLowerInvariant();
        if (!SafeExtensions.Contains(extension))
        {
            extension = ".jpg";
        }

        return $"{Guid.NewGuid():N}{extension}";
    }

    public static string ContentTypeFor(string fileName) => Path.GetExtension(fileName).ToLowerInvariant() switch
    {
        ".png" => "image/png",
        ".webp" => "image/webp",
        _ => "image/jpeg"
    };

    /// <summary>Splits "private:receipts/xxxx.jpg" into folder and file; false for anything that isn't exactly that shape.</summary>
    public static bool TryParsePrivate(string? reference, out string folder, out string file)
    {
        var match = PrivatePattern.Match(reference ?? string.Empty);
        folder = match.Success ? match.Groups["folder"].Value : string.Empty;
        file = match.Success ? match.Groups["file"].Value : string.Empty;
        return match.Success;
    }

    /// <summary>Splits "products/xxxx.jpg" (no leading slash) into folder and file; false if it isn't a file we generated.</summary>
    public static bool TryParsePublicObject(string? objectPath, out string folder, out string file)
    {
        var match = PublicObjectPattern.Match(objectPath ?? string.Empty);
        folder = match.Success ? match.Groups["folder"].Value : string.Empty;
        file = match.Success ? match.Groups["file"].Value : string.Empty;
        return match.Success;
    }
}
