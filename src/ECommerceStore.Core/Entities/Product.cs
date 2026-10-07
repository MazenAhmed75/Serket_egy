using ECommerceStore.Core.Constants;

namespace ECommerceStore.Core.Entities;

/// <summary>
/// A catalog product (one fabric of scrubs). Every active product gets a card on the home page and
/// its own product page at /product/{slug}. Colours, extra photos and stock belong to the product.
/// </summary>
public class Product
{
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;

    /// <summary>URL-friendly unique name used in the product page address, e.g. "rozalin".</summary>
    public string Slug { get; set; } = string.Empty;

    /// <summary>One short line shown on the product's card on the home page (optional).</summary>
    public string? ShortDescription { get; set; }

    /// <summary>Lower numbers are shown first on the home page.</summary>
    public int SortOrder { get; set; }

    /// <summary>Percentage taken off <see cref="Price"/> while <see cref="DiscountActive"/> is on (1-99).</summary>
    public int DiscountPercent { get; set; }

    /// <summary>The owner's on/off switch: the discount banner and the lower price only apply while this is true.</summary>
    public bool DiscountActive { get; set; }

    public string Description { get; set; } = string.Empty;

    public decimal Price { get; set; }

    public string ImageUrl { get; set; } = string.Empty;

    public bool IsActive { get; set; }

    public DateTime CreatedAt { get; set; }

    // Navigation
    public ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();

    /// <summary>Optional colour options. Empty means this product has no colour picker.</summary>
    public ICollection<ProductColor> Colors { get; set; } = new List<ProductColor>();

    /// <summary>Optional extra gallery photos (the main photo is <see cref="ImageUrl"/>).</summary>
    public ICollection<ProductImage> Images { get; set; } = new List<ProductImage>();

    /// <summary>Units on hand per colour and size.</summary>
    public ICollection<ProductStock> Stock { get; set; } = new List<ProductStock>();

    public ICollection<Review> Reviews { get; set; } = new List<Review>();

    /// <summary>True while a valid discount is switched on.</summary>
    public bool HasDiscount => DiscountActive && DiscountPercent is > 0 and < 100;

    /// <summary>What a customer pays per unit: <see cref="Price"/> minus the discount when one is active.</summary>
    public decimal CurrentPrice => HasDiscount
        ? Math.Round(Price * (100 - DiscountPercent) / 100m, 2, MidpointRounding.AwayFromZero)
        : Price;

    // ---- Stock rules (kept on the entity so pages, views and services all agree) ----

    /// <summary>Units that can be sold right now: stock of active colours (or colour-less stock when there are no colours).</summary>
    public int UnitsInStock => Stock
        .Where(s => s.ProductColorId == null || Colors.Any(c => c.Id == s.ProductColorId && c.IsActive))
        .Sum(s => Math.Max(s.Quantity, 0));

    public bool IsInStock => UnitsInStock > 0;

    /// <summary>Units on hand for one colour and size (0 when there is no stock row).</summary>
    public int UnitsAvailable(Guid? colorId, string size) =>
        Math.Max(Stock.Where(s => s.ProductColorId == colorId && s.Size == size).Sum(s => s.Quantity), 0);

    /// <summary>Units for every sellable size of one colour, 0 where no stock row exists.</summary>
    public IReadOnlyDictionary<string, int> UnitsBySize(Guid? colorId) =>
        ProductSizes.All.ToDictionary(size => size, size => UnitsAvailable(colorId, size));
}
