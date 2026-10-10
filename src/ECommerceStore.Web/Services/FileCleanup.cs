using ECommerceStore.Core.Exceptions;
using ECommerceStore.Core.Interfaces;

namespace ECommerceStore.Web.Services;

/// <summary>
/// Deletes stored files after the database no longer points to them (a removed photo, a replaced photo, a deleted
/// product). This is best effort: if the storage service is unreachable the page still succeeds and the problem is
/// only logged, because an unused leftover file is harmless while a failed page would not be.
/// </summary>
public class FileCleanup
{
    private readonly IFileStorageService _storage;
    private readonly ILogger<FileCleanup> _logger;

    public FileCleanup(IFileStorageService storage, ILogger<FileCleanup> logger)
    {
        _storage = storage;
        _logger = logger;
    }

    public async Task DeleteQuietlyAsync(params string?[] references)
    {
        foreach (var reference in references.Where(r => !string.IsNullOrEmpty(r)))
        {
            try
            {
                await _storage.DeleteAsync(reference);
            }
            catch (FileStorageException ex)
            {
                _logger.LogWarning(ex, "Could not delete stored file {Reference}; it can be removed by hand.", reference);
            }
        }
    }
}
