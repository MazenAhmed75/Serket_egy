namespace ECommerceStore.Core.Entities;

/// <summary>
/// An extra gallery photo for a <see cref="Product"/>. The main photo stays in
/// <see cref="Product.ImageUrl"/>; rows here are the additional angles shown as thumbnails.
/// </summary>
public class ProductImage
{
    public Guid Id { get; set; }

    public Guid ProductId { get; set; }

    /// <summary>Web-relative path, e.g. "/uploads/products/xxxx.jpg".</summary>
    public string ImageUrl { get; set; } = string.Empty;

    /// <summary>Lower numbers are shown first.</summary>
    public int SortOrder { get; set; }

    // Navigation
    public Product Product { get; set; } = null!;
}
