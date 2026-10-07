namespace ECommerceStore.Core.DTOs;

/// <summary>A review as shown on the product page.</summary>
public sealed record ReviewDto(string AuthorName, int Rating, string? Comment, DateTime CreatedAt);
