using ECommerceStore.Core.Constants;
using ECommerceStore.Core.Entities;
using ECommerceStore.Core.Services;
using Microsoft.EntityFrameworkCore;

namespace ECommerceStore.Infrastructure.Data;

/// <summary>
/// Startup seed. Safe to call on every startup: each step only fills in what is missing.
///  - an empty database gets the three starter fabrics (edit them from the Admin area afterwards);
///  - products created before product pages existed get a slug (their page address);
///  - products with no stock rows get placeholder stock so nothing shows as sold out by surprise;
///  - the shipping-fee settings row is created.
/// </summary>
public static class DbSeeder
{
    public static async Task SeedAsync(AppDbContext context, CancellationToken cancellationToken = default)
    {
        if (!await context.Products.AnyAsync(cancellationToken))
        {
            context.Products.AddRange(StarterCatalog.BuildMissing(Array.Empty<string>()));
            await context.SaveChangesAsync(cancellationToken);
        }

        // Products that existed before slugs were introduced have an empty slug; give each a unique one.
        var withoutSlug = await context.Products
            .Where(p => p.Slug == "")
            .ToListAsync(cancellationToken);

        if (withoutSlug.Count > 0)
        {
            var taken = await context.Products
                .Where(p => p.Slug != "")
                .Select(p => p.Slug)
                .ToListAsync(cancellationToken);
            var used = new HashSet<string>(taken);

            foreach (var product in withoutSlug)
            {
                product.Slug = await Slugger.MakeUniqueAsync(product.Name, slug => Task.FromResult(used.Contains(slug)));
                used.Add(product.Slug);
            }

            await context.SaveChangesAsync(cancellationToken);
        }

        // Stock is tracked per colour and size. A product that has no stock rows yet gets placeholder
        // rows; the owner then sets the real numbers in Admin > Products > (product) > Stock by size.
        var productsWithoutStock = await context.Products
            .Include(p => p.Colors)
            .Where(p => !p.Stock.Any())
            .ToListAsync(cancellationToken);

        foreach (var product in productsWithoutStock)
        {
            var colorIds = product.Colors.Where(c => c.IsActive).Select(c => (Guid?)c.Id).ToList();
            if (colorIds.Count == 0)
            {
                colorIds.Add(null);
            }

            foreach (var colorId in colorIds)
            {
                foreach (var size in ProductSizes.All)
                {
                    context.ProductStocks.Add(new ProductStock
                    {
                        Id = Guid.NewGuid(),
                        ProductId = product.Id,
                        ProductColorId = colorId,
                        Size = size,
                        Quantity = StarterCatalog.PlaceholderStock
                    });
                }
            }
        }

        if (!await context.StoreSettings.AnyAsync(cancellationToken))
        {
            context.StoreSettings.Add(new StoreSettings());
        }

        await context.SaveChangesAsync(cancellationToken);
    }
}
