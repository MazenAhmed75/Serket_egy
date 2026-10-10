namespace ECommerceStore.Core.Entities;

/// <summary>
/// An optional colour option for a <see cref="Product"/>. Stock is tracked per size in
/// <see cref="ProductStock"/>. A product with zero colours shows no colour picker.
/// </summary>
public class ProductColor
{
    public Guid Id { get; set; }

    public Guid ProductId { get; set; }

    /// <summary>Display name, e.g. "Black", "Ceil Blue".</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>CSS colour used to draw the swatch, e.g. "#1A1A1A".</summary>
    public string HexCode { get; set; } = string.Empty;

    /// <summary>Optional photo of the set in this colour; shown when the customer picks the colour.</summary>
    public string? ImageUrl { get; set; }

    public bool IsActive { get; set; } = true;

    // Navigation
    public Product Product { get; set; } = null!;

    /// <summary>The extra photos shown when this colour is chosen.</summary>
    public ICollection<ProductImage> Images { get; set; } = new List<ProductImage>();
}
