namespace ECommerceStore.Core.DTOs;

/// <summary>What a product's card on the home page needs.</summary>
public class ProductCardDto
{
    public string Slug { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string? ShortDescription { get; set; }

    /// <summary>What a customer pays (already reduced when a discount is active).</summary>
    public decimal Price { get; set; }

    /// <summary>The price before the discount; only different from <see cref="Price"/> while a discount is active.</summary>
    public decimal OriginalPrice { get; set; }

    /// <summary>0 when no discount is showing.</summary>
    public int DiscountPercent { get; set; }

    public RatingSummary Rating { get; set; } = RatingSummary.None;

    public string ImageUrl { get; set; } = string.Empty;

    public bool InStock { get; set; }

    /// <summary>Hex codes of the colours it comes in (shown as small dots on the card).</summary>
    public IReadOnlyList<string> Swatches { get; set; } = Array.Empty<string>();
}
