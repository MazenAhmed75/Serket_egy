namespace ECommerceStore.Core.Constants;

/// <summary>Single source of truth for the sizes the store sells (used by validation, stock and the UI).</summary>
public static class ProductSizes
{
    public static readonly IReadOnlyList<string> All = new[] { "M", "L", "XL", "2XL" };

    public const string Default = "M";

    public static bool IsValid(string? size) => size is not null && All.Contains(size);
}
