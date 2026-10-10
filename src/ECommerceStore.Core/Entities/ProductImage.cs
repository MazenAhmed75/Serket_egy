namespace ECommerceStore.Core.Entities;

/// <summary>
/// An extra gallery photo for one colour of a <see cref="Product"/>. The colour's own main photo stays in
/// <see cref="ProductColor.ImageUrl"/>; rows here are the additional angles shown as thumbnails when that colour is chosen.
/// </summary>
public class ProductImage
{
    public Guid Id { get; set; }

    public Guid ProductId { get; set; }

    /// <summary>The colour this photo belongs to. Null only for old photos from before galleries were per colour (no longer shown).</summary>
    public Guid? ProductColorId { get; set; }

    /// <summary>Web-relative path, e.g. "/uploads/products/xxxx.jpg".</summary>
    public string ImageUrl { get; set; } = string.Empty;

    /// <summary>Lower numbers are shown first.</summary>
    public int SortOrder { get; set; }

    // Navigation
    public Product Product { get; set; } = null!;

    public ProductColor? ProductColor { get; set; }
}
