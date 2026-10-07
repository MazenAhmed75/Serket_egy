using ECommerceStore.Core.Entities;

namespace ECommerceStore.Core.Interfaces;

public interface IProductRepository : IRepository<Product>
{
    /// <summary>Every active product in display order, with colours, extra photos and stock loaded (home page).</summary>
    Task<IReadOnlyList<Product>> GetActiveProductsAsync();

    /// <summary>The active product with this slug, with colours, extra photos and stock loaded (product page).</summary>
    Task<Product?> GetActiveBySlugAsync(string slug);

    /// <summary>Loads a product (active or not) together with its colours, extra photos and stock.</summary>
    Task<Product?> GetWithColorsAsync(Guid id);

    /// <summary>Every product (active or not) in display order, with colours and stock loaded (Admin list).</summary>
    Task<IReadOnlyList<Product>> GetAllWithDetailsAsync();

    /// <summary>
    /// Takes <paramref name="quantity"/> units out of one colour/size in a single atomic step that only succeeds when
    /// enough units are left, so two customers can never both buy the last unit. Returns false when there is not enough.
    /// </summary>
    Task<bool> TryDeductStockAsync(Guid productId, Guid? colorId, string size, int quantity);

    /// <summary>Puts units back into a colour/size (for example when an order is cancelled). Does nothing if that stock row no longer exists.</summary>
    Task RestoreStockAsync(Guid productId, Guid? colorId, string size, int quantity);

    Task<bool> SlugExistsAsync(string slug, Guid? excludeProductId = null);

    /// <summary>True when any order contains this product (such a product can be hidden but not deleted).</summary>
    Task<bool> HasOrdersAsync(Guid productId);
}
