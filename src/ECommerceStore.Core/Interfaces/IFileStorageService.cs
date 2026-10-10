namespace ECommerceStore.Core.Interfaces;

/// <summary>
/// Persists uploaded files. Product photos are public; payment receipts are private (never reachable by a web
/// address, only through the admin-only receipt page). Implementations: local disk (development) and Supabase Storage (production).
/// All failures surface as <see cref="Exceptions.FileStorageException"/>.
/// </summary>
public interface IFileStorageService
{
    /// <summary>
    /// Saves a public image and returns what to store in the database and put in an img tag: a web path
    /// ("/uploads/products/xxxx.jpg") or a full URL. The file name is generated; only a safe image extension is kept.
    /// </summary>
    Task<string> SaveAsync(
        Stream content,
        string originalFileName,
        string subFolder,
        CancellationToken cancellationToken = default);

    /// <summary>Saves a private image and returns a reference such as "private:receipts/xxxx.jpg" (stored instead of a web path).</summary>
    Task<string> SavePrivateAsync(
        Stream content,
        string originalFileName,
        string subFolder,
        CancellationToken cancellationToken = default);

    /// <summary>Opens a file saved with <see cref="SavePrivateAsync"/>; null when the reference is invalid or the file is gone.</summary>
    Task<Stream?> OpenPrivateAsync(string reference, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a file this service saved (a public path/URL or a "private:" reference). Anything that is not one of our
    /// own generated files (for example the starter photos shipped with the site) is ignored. A file that is already gone counts as deleted.
    /// </summary>
    Task DeleteAsync(string? reference, CancellationToken cancellationToken = default);
}
