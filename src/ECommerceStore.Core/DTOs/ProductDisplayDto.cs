namespace ECommerceStore.Core.DTOs;

/// <summary>Read-only shape used to render the product on the storefront.</summary>
public class ProductDisplayDto
{
    public Guid Id { get; set; }

    public string Slug { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    /// <summary>What a customer pays (already reduced when a discount is active).</summary>
    public decimal Price { get; set; }

    /// <summary>The price before the discount; only different from <see cref="Price"/> while a discount is active.</summary>
    public decimal OriginalPrice { get; set; }

    /// <summary>0 when no discount is showing.</summary>
    public int DiscountPercent { get; set; }

    public RatingSummary Rating { get; set; } = RatingSummary.None;

    public IReadOnlyList<ReviewDto> Reviews { get; set; } = Array.Empty<ReviewDto>();

    public string ImageUrl { get; set; } = string.Empty;

    /// <summary>Main photo first, then any extra gallery photos in display order.</summary>
    public IReadOnlyList<string> GalleryImages { get; set; } = Array.Empty<string>();

    /// <summary>Empty when this product has no colour options — the storefront shows no picker in that case.</summary>
    public IReadOnlyList<ProductColorDto> Colors { get; set; } = Array.Empty<ProductColorDto>();

    /// <summary>Units per size when the product has no colour options.</summary>
    public IReadOnlyDictionary<string, int> DefaultStock { get; set; } = new Dictionary<string, int>();

    public bool HasColors => Colors.Count > 0;

    public bool InStock => HasColors ? Colors.Any(c => c.InStock) : DefaultStock.Values.Any(q => q > 0);
}
