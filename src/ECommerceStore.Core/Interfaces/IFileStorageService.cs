namespace ECommerceStore.Core.Interfaces;

/// <summary>
/// Persists uploaded files. Product photos are public (served as static files); payment receipts are private
/// (stored outside the web root and only readable through an admin-only page).
/// </summary>
public interface IFileStorageService
{
    /// <summary>
    /// Saves a public image under wwwroot/uploads/<paramref name="subFolder"/> and returns the web-relative
    /// path (e.g. "/uploads/products/xxxx.jpg"). The file name is generated; only a safe image extension is kept.
    /// </summary>
    Task<string> SaveAsync(
        Stream content,
        string originalFileName,
        string subFolder,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Saves a private image outside the web root and returns a reference such as "private:receipts/xxxx.jpg"
    /// (stored in the database instead of a web path).
    /// </summary>
    Task<string> SavePrivateAsync(
        Stream content,
        string originalFileName,
        string subFolder,
        CancellationToken cancellationToken = default);

    /// <summary>Opens a file saved with <see cref="SavePrivateAsync"/>; null when the reference is invalid or the file is gone.</summary>
    Stream? OpenPrivate(string reference);
}
