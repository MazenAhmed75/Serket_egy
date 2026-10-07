namespace ECommerceStore.Core.Constants;

/// <summary>The fits the store sells ("Male" = men's fit, "Female" = women's fit). Used by validation and the UI.</summary>
public static class ProductGenders
{
    public static readonly IReadOnlyList<string> All = new[] { "Male", "Female" };

    public const string Default = "Male";

    public static bool IsValid(string? gender) => gender is not null && All.Contains(gender);
}
