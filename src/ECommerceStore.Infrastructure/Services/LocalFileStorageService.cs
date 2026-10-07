using System.Text.RegularExpressions;
using ECommerceStore.Core.Interfaces;

namespace ECommerceStore.Infrastructure.Services;

/// <summary>
/// Saves uploaded files to disk. Public images go under wwwroot/uploads/{subFolder} (served as static files);
/// private images (payment receipts) go under a folder outside the web root. File names are always generated
/// and the extension is limited to a safe image type, so an upload can never be saved as, say, a web page or script.
/// Swap this for a cloud-storage implementation (S3/Azure Blob) later without touching any calling code.
/// </summary>
public class LocalFileStorageService : IFileStorageService
{
    private const string PrivatePrefix = "private:";

    private static readonly HashSet<string> SafeExtensions = new(StringComparer.OrdinalIgnoreCase) { ".jpg", ".jpeg", ".png", ".webp" };

    // "private:receipts/<32 hex characters>.<jpg|jpeg|png|webp>"; nothing else is ever opened.
    private static readonly Regex PrivateReferencePattern =
        new(@"^private:(?<folder>[a-z]{1,30})/(?<file>[0-9a-f]{32}\.(jpg|jpeg|png|webp))$", RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private readonly string _webRootPath;
    private readonly string _privateRootPath;

    public LocalFileStorageService(string webRootPath, string privateRootPath)
    {
        _webRootPath = webRootPath;
        _privateRootPath = privateRootPath;
    }

    public async Task<string> SaveAsync(
        Stream content,
        string originalFileName,
        string subFolder,
        CancellationToken cancellationToken = default)
    {
        var fileName = NewFileName(originalFileName);
        await WriteAsync(Path.Combine(_webRootPath, "uploads", subFolder), fileName, content, cancellationToken);
        return $"/uploads/{subFolder}/{fileName}";
    }

    public async Task<string> SavePrivateAsync(
        Stream content,
        string originalFileName,
        string subFolder,
        CancellationToken cancellationToken = default)
    {
        var fileName = NewFileName(originalFileName);
        await WriteAsync(Path.Combine(_privateRootPath, subFolder), fileName, content, cancellationToken);
        return $"{PrivatePrefix}{subFolder}/{fileName}";
    }

    public Stream? OpenPrivate(string reference)
    {
        var match = PrivateReferencePattern.Match(reference ?? string.Empty);
        if (!match.Success)
        {
            return null;
        }

        var fullPath = Path.Combine(_privateRootPath, match.Groups["folder"].Value, match.Groups["file"].Value);
        return File.Exists(fullPath) ? new FileStream(fullPath, FileMode.Open, FileAccess.Read, FileShare.Read) : null;
    }

    private static string NewFileName(string originalFileName)
    {
        var extension = Path.GetExtension(originalFileName).ToLowerInvariant();
        if (!SafeExtensions.Contains(extension))
        {
            extension = ".jpg";
        }

        return $"{Guid.NewGuid():N}{extension}";
    }

    private static async Task WriteAsync(string folderPath, string fileName, Stream content, CancellationToken cancellationToken)
    {
        Directory.CreateDirectory(folderPath);

        await using var fileStream = new FileStream(Path.Combine(folderPath, fileName), FileMode.CreateNew, FileAccess.Write);
        await content.CopyToAsync(fileStream, cancellationToken);
    }
}
