namespace ECommerceStore.Core.DTOs;

/// <summary>A product's average star rating and how many customers rated it.</summary>
public sealed record RatingSummary(double Average, int Count)
{
    public static readonly RatingSummary None = new(0, 0);
}
