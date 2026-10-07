namespace ECommerceStore.Core.Entities;

public class OrderItem
{
    public Guid Id { get; set; }

    public Guid OrderId { get; set; }

    public Guid ProductId { get; set; }

    public int Quantity { get; set; }

    /// <summary>Unit price captured at the time of the order (kept independent of the live <see cref="Product.Price"/>).</summary>
    public decimal UnitPrice { get; set; }

    /// <summary>Set only when the product had colour options at the time of order.</summary>
    public Guid? ProductColorId { get; set; }

    /// <summary>Snapshot of the colour's name, so the order still shows it even if the colour is later renamed or removed.</summary>
    public string? ColorName { get; set; }

    /// <summary>Selected gender (e.g. "Male", "Female").</summary>
    public string? Gender { get; set; }

    /// <summary>Selected size (e.g. "S", "M", "L", "XL", "XXL").</summary>
    public string? Size { get; set; }

    // Navigation
    public Order Order { get; set; } = null!;

    public Product Product { get; set; } = null!;

    public ProductColor? ProductColor { get; set; }
}
