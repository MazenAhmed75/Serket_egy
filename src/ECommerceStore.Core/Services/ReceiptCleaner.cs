using ECommerceStore.Core.Exceptions;
using ECommerceStore.Core.Interfaces;

namespace ECommerceStore.Core.Services;

/// <summary>How many receipt images were deleted, and how many could not be (they are retried next time).</summary>
public sealed record ReceiptCleanupResult(int Deleted, int Failed);

/// <summary>
/// Deletes the screenshot files of receipts that are no longer needed (orders already shipped or cancelled) to save
/// storage space and keep customers' payment details no longer than necessary. The receipt record itself stays.
/// </summary>
public class ReceiptCleaner
{
    private const int BatchSize = 50;
    private const int MaxPerRun = 1000;

    private readonly IUnitOfWork _unitOfWork;
    private readonly IFileStorageService _fileStorage;

    public ReceiptCleaner(IUnitOfWork unitOfWork, IFileStorageService fileStorage)
    {
        _unitOfWork = unitOfWork;
        _fileStorage = fileStorage;
    }

    /// <summary>Deletes receipt images of finished orders placed before <paramref name="createdBeforeUtc"/>.</summary>
    public async Task<ReceiptCleanupResult> DeleteImagesAsync(DateTime createdBeforeUtc, CancellationToken cancellationToken = default)
    {
        var deleted = 0;
        var failedIds = new HashSet<Guid>();

        while (deleted + failedIds.Count < MaxPerRun)
        {
            var receipts = await _unitOfWork.Orders.GetReceiptsWithImagesForFinishedOrdersAsync(createdBeforeUtc, BatchSize);

            // Receipts that already failed in this run come back in the next batch; don't try them again.
            var pending = receipts.Where(r => !failedIds.Contains(r.Id)).ToList();
            if (pending.Count == 0)
            {
                break;
            }

            var deletedInBatch = 0;
            foreach (var receipt in pending)
            {
                try
                {
                    await _fileStorage.DeleteAsync(receipt.ImagePath, cancellationToken);
                    receipt.ImagePath = string.Empty;
                    deletedInBatch++;
                }
                catch (FileStorageException)
                {
                    failedIds.Add(receipt.Id);
                }
            }

            await _unitOfWork.SaveChangesAsync(cancellationToken);
            deleted += deletedInBatch;

            // Nothing could be deleted (storage unreachable): stop instead of asking for the same batch again.
            if (deletedInBatch == 0)
            {
                break;
            }
        }

        var failed = failedIds.Count;
        return new ReceiptCleanupResult(deleted, failed);
    }
}
