namespace ECommerceStore.Core.Entities;

/// <summary>
/// Units on hand for one size of one colour of a product (a "black / M" and a "blue / M" are
/// different items). <see cref="ProductColorId"/> is null for products that have no colours.
/// </summary>
public class ProductStock
{
    public Guid Id { get; set; }

    public Guid ProductId { get; set; }

    public Guid? ProductColorId { get; set; }

    /// <summary>One of <see cref="Constants.ProductSizes.All"/>.</summary>
    public string Size { get; set; } = string.Empty;

    public int Quantity { get; set; }

    // Navigation
    public Product Product { get; set; } = null!;

    public ProductColor? ProductColor { get; set; }
}
