using ECommerceStore.Core.Constants;
using ECommerceStore.Core.Entities;

namespace ECommerceStore.Core.Services;

/// <summary>
/// The three fabrics the store launches with. The blue, maroon and grey photos ship with the site
/// (wwwroot/images/starter); the black fabric still uses a placeholder until its photo is uploaded in
/// Admin &gt; Products. Stock starts at <see cref="PlaceholderStock"/> units per colour and size until the real numbers are entered.
/// </summary>
public static class StarterCatalog
{
    public const int PlaceholderStock = 10;

    public const string PlaceholderImage = "/images/product-placeholder.jpg";

    private const string BluePhoto = "/images/starter/rozalin-blue.jpg";
    private const string MaroonPhoto = "/images/starter/waterproof-maroon.jpg";
    private const string GreyPhoto = "/images/starter/waterproof-grey.jpg";

    /// <summary>Builds the starter products whose slug is not in <paramref name="existingSlugs"/>.</summary>
    public static List<Product> BuildMissing(IReadOnlyCollection<string> existingSlugs)
    {
        var products = new List<Product>();

        if (!existingSlugs.Contains("rozalin"))
        {
            products.Add(Create(
                slug: "rozalin",
                name: "Rozalin Scrub Set",
                shortDescription: "Soft, lightweight fabric for easy movement",
                description: "Made from Rozalin fabric, a soft, lightweight fabric designed to provide lasting comfort, " +
                             "smooth wear and easy movement, while maintaining a polished, professional appearance. " +
                             "Available in blue.",
                price: 1150m,
                sortOrder: 1,
                imageUrl: BluePhoto,
                colors: new[] { ("Blue", "#1F2F8F", (string?)BluePhoto) }));
        }

        if (!existingSlugs.Contains("antibacterial"))
        {
            products.Add(Create(
                slug: "antibacterial",
                name: "Antibacterial Scrub Set",
                shortDescription: "Soft, antibacterial fabric",
                description: "Scrubs made from a soft, antibacterial fabric, so you stay comfortable while you wear them. " +
                             "Available in black.",
                price: 1300m,
                sortOrder: 2,
                imageUrl: PlaceholderImage,
                colors: new[] { ("Black", "#1A1A1A", (string?)null) }));
        }

        if (!existingSlugs.Contains("waterproof-antibacterial"))
        {
            products.Add(Create(
                slug: "waterproof-antibacterial",
                name: "Waterproof Antibacterial Scrub Set",
                shortDescription: "Soft, waterproof and antibacterial",
                description: "Scrubs made from a soft fabric that is both waterproof and antibacterial. " +
                             "Available in maroon and grey.",
                price: 1300m,
                sortOrder: 3,
                imageUrl: MaroonPhoto,
                colors: new[] { ("Maroon", "#6D1F2F", (string?)MaroonPhoto), ("Grey", "#8C9096", (string?)GreyPhoto) }));
        }

        return products;
    }

    private static Product Create(
        string slug,
        string name,
        string shortDescription,
        string description,
        decimal price,
        int sortOrder,
        string imageUrl,
        (string Name, string Hex, string? ImageUrl)[] colors)
    {
        var product = new Product
        {
            Id = Guid.NewGuid(),
            Slug = slug,
            Name = name,
            ShortDescription = shortDescription,
            Description = description,
            Price = price,
            ImageUrl = imageUrl,
            SortOrder = sortOrder,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        foreach (var (colorName, hex, colorImage) in colors)
        {
            var color = new ProductColor
            {
                Id = Guid.NewGuid(),
                ProductId = product.Id,
                Name = colorName,
                HexCode = hex,
                ImageUrl = colorImage,
                IsActive = true
            };
            product.Colors.Add(color);

            foreach (var size in ProductSizes.All)
            {
                product.Stock.Add(new ProductStock
                {
                    Id = Guid.NewGuid(),
                    ProductId = product.Id,
                    ProductColorId = color.Id,
                    Size = size,
                    Quantity = PlaceholderStock
                });
            }
        }

        return product;
    }
}
