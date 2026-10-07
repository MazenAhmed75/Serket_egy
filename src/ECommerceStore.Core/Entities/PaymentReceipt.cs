namespace ECommerceStore.Core.Entities;

/// <summary>
/// A receipt screenshot (Vodafone Cash / InstaPay) uploaded by the customer for admin verification.
/// An order can accumulate more than one of these (e.g. a rejected attempt followed by a corrected upload).
/// </summary>
public class PaymentReceipt
{
    public Guid Id { get; set; }

    public Guid OrderId { get; set; }

    /// <summary>Relative path/URL of the stored screenshot (see <c>IFileStorageService</c> in Infrastructure).</summary>
    public string ImagePath { get; set; } = string.Empty;

    public string? TransactionReference { get; set; }

    public DateTime UploadedAt { get; set; }

    public bool IsVerified { get; set; }

    // Navigation
    public Order Order { get; set; } = null!;
}
