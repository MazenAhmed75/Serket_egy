namespace ECommerceStore.Core.Entities;

/// <summary>
/// A receipt screenshot (Vodafone Cash / InstaPay) uploaded by the customer for admin verification.
/// An order can accumulate more than one of these (e.g. a rejected attempt followed by a corrected upload).
/// </summary>
public class PaymentReceipt
{
    public Guid Id { get; set; }

    public Guid OrderId { get; set; }

    /// <summary>
    /// Where the screenshot is stored (see <c>IFileStorageService</c>). Empty once the image has been deleted
    /// (by the owner, or by the automatic clean-up); the reference and verified flag are kept.
    /// </summary>
    public string ImagePath { get; set; } = string.Empty;

    public bool HasImage => !string.IsNullOrEmpty(ImagePath);

    public string? TransactionReference { get; set; }

    public DateTime UploadedAt { get; set; }

    public bool IsVerified { get; set; }

    // Navigation
    public Order Order { get; set; } = null!;
}
