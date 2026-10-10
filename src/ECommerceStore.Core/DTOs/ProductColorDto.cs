namespace ECommerceStore.Core.DTOs;

public class ProductColorDto
{
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string HexCode { get; set; } = string.Empty;

    /// <summary>Photo of the set in this colour (null when the owner has not added one).</summary>
    public string? ImageUrl { get; set; }

    /// <summary>Every photo shown for this colour, in order: its main photo first, then its gallery.</summary>
    public IReadOnlyList<string> Photos { get; set; } = Array.Empty<string>();

    /// <summary>Units on hand per size (every size in <see cref="Constants.ProductSizes.All"/> is present).</summary>
    public IReadOnlyDictionary<string, int> Stock { get; set; } = new Dictionary<string, int>();

    public bool InStock => Stock.Values.Any(q => q > 0);
}
