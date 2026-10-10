using ECommerceStore.Core.Exceptions;
using ECommerceStore.Core.Interfaces;
using ECommerceStore.Core.Services;

namespace ECommerceStore.Infrastructure.Services;

/// <summary>
/// Saves uploaded files to disk (used for development, or when no cloud storage is configured). Public images go
/// under wwwroot/uploads/{subFolder}; private images (payment receipts) go under a folder outside the web root.
/// File names are generated and only a safe image extension is kept, so an upload can never be saved as, say, a web page.
/// Note: on hosts whose disk is erased on every restart (such as Render without a persistent disk) use Supabase storage instead.
/// </summary>
public class LocalFileStorageService : IFileStorageService
{
    private readonly string _webRootPath;
    private readonly string _privateRootPath;

    public LocalFileStorageService(string webRootPath, string privateRootPath)
    {
        _webRootPath = webRootPath;
        _privateRootPath = privateRootPath;
    }

    public async Task<string> SaveAsync(Stream content, string originalFileName, string subFolder, CancellationToken cancellationToken = default)
    {
        var fileName = StoredFileNames.NewFileName(originalFileName);
        await WriteAsync(Path.Combine(_webRootPath, "uploads", subFolder), fileName, content, cancellationToken);
        return $"/uploads/{subFolder}/{fileName}";
    }

    public async Task<string> SavePrivateAsync(Stream content, string originalFileName, string subFolder, CancellationToken cancellationToken = default)
    {
        var fileName = StoredFileNames.NewFileName(originalFileName);
        await WriteAsync(Path.Combine(_privateRootPath, subFolder), fileName, content, cancellationToken);
        return $"{StoredFileNames.PrivatePrefix}{subFolder}/{fileName}";
    }

    public Task<Stream?> OpenPrivateAsync(string reference, CancellationToken cancellationToken = default)
    {
        if (!StoredFileNames.TryParsePrivate(reference, out var folder, out var file))
        {
            return Task.FromResult<Stream?>(null);
        }

        var fullPath = Path.Combine(_privateRootPath, folder, file);
        Stream? stream = File.Exists(fullPath) ? new FileStream(fullPath, FileMode.Open, FileAccess.Read, FileShare.Read) : null;
        return Task.FromResult(stream);
    }

    public Task DeleteAsync(string? reference, CancellationToken cancellationToken = default)
    {
        string? fullPath = null;

        if (StoredFileNames.TryParsePrivate(reference, out var privateFolder, out var privateFile))
        {
            fullPath = Path.Combine(_privateRootPath, privateFolder, privateFile);
        }
        else if (reference is not null && reference.StartsWith("/uploads/", StringComparison.Ordinal) &&
                 StoredFileNames.TryParsePublicObject(reference["/uploads/".Length..], out var folder, out var file))
        {
            fullPath = Path.Combine(_webRootPath, "uploads", folder, file);
        }

        // Anything that is not one of our generated files (starter photos, other paths) is left alone.
        if (fullPath is not null)
        {
            try
            {
                File.Delete(fullPath); // a file that is already gone is not an error
            }
            catch (IOException ex)
            {
                throw new FileStorageException("The file could not be deleted.", ex);
            }
            catch (UnauthorizedAccessException ex)
            {
                throw new FileStorageException("The file could not be deleted.", ex);
            }
        }

        return Task.CompletedTask;
    }

    private static async Task WriteAsync(string folderPath, string fileName, Stream content, CancellationToken cancellationToken)
    {
        try
        {
            Directory.CreateDirectory(folderPath);

            await using var fileStream = new FileStream(Path.Combine(folderPath, fileName), FileMode.CreateNew, FileAccess.Write);
            await content.CopyToAsync(fileStream, cancellationToken);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            throw new FileStorageException("The file could not be saved.", ex);
        }
    }
}
