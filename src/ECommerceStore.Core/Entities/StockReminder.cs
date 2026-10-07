namespace ECommerceStore.Core.Entities;

/// <summary>
/// "Remind me when it is back": a customer's e-mail waiting for one colour and size of a product to be
/// restocked. <see cref="NotifiedAt"/> is set once the e-mail has been sent.
/// </summary>
public class StockReminder
{
    public Guid Id { get; set; }

    public Guid ProductId { get; set; }

    /// <summary>Null for products that have no colours.</summary>
    public Guid? ProductColorId { get; set; }

    public string Size { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }

    public DateTime? NotifiedAt { get; set; }

    // Navigation
    public Product Product { get; set; } = null!;

    public ProductColor? ProductColor { get; set; }
}
