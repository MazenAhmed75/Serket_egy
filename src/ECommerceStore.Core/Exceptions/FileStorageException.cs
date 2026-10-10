namespace ECommerceStore.Core.Exceptions;

/// <summary>
/// Saving, reading or deleting a stored file failed (for example the storage service was unreachable).
/// The message is safe to log and never contains secrets.
/// </summary>
public class FileStorageException : Exception
{
    public FileStorageException(string message) : base(message)
    {
    }

    public FileStorageException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
