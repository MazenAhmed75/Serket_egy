using ECommerceStore.Core.Entities;
using ECommerceStore.Core.Interfaces;
using ECommerceStore.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ECommerceStore.Infrastructure.Repositories;

public class ProductRepository : Repository<Product>, IProductRepository
{
    public ProductRepository(AppDbContext context) : base(context)
    {
    }

    private IQueryable<Product> WithDetails() =>
        DbSet
            .Include(p => p.Colors)
            .Include(p => p.Images)
            .Include(p => p.Stock);

    // The public pages only read products, so they skip change tracking (faster, less memory per visitor).
    public async Task<IReadOnlyList<Product>> GetActiveProductsAsync() =>
        await WithDetails()
            .AsNoTracking()
            .Where(p => p.IsActive)
            .OrderBy(p => p.SortOrder)
            .ThenBy(p => p.Name)
            .ToListAsync();

    public async Task<Product?> GetActiveBySlugAsync(string slug) =>
        await WithDetails().AsNoTracking().FirstOrDefaultAsync(p => p.IsActive && p.Slug == slug);

    public async Task<Product?> GetWithColorsAsync(Guid id) =>
        await WithDetails().FirstOrDefaultAsync(p => p.Id == id);

    public async Task<IReadOnlyList<Product>> GetAllWithDetailsAsync() =>
        await WithDetails()
            .OrderBy(p => p.SortOrder)
            .ThenBy(p => p.Name)
            .ToListAsync();

    public async Task<bool> TryDeductStockAsync(Guid productId, Guid? colorId, string size, int quantity)
    {
        // One UPDATE ... WHERE Quantity >= @quantity: the database does the check and the subtraction together,
        // so concurrent orders can't both pass a stale "is there enough?" check.
        var rows = await Context.ProductStocks
            .Where(s => s.ProductId == productId && s.ProductColorId == colorId && s.Size == size && s.Quantity >= quantity)
            .ExecuteUpdateAsync(set => set.SetProperty(s => s.Quantity, s => s.Quantity - quantity));

        if (rows > 1)
        {
            // Two stock rows for the same colour/size would mean double-counting; stop (the caller's transaction rolls back).
            throw new InvalidOperationException("More than one stock row exists for the same product, colour and size.");
        }

        return rows == 1;
    }

    public async Task RestoreStockAsync(Guid productId, Guid? colorId, string size, int quantity) =>
        await Context.ProductStocks
            .Where(s => s.ProductId == productId && s.ProductColorId == colorId && s.Size == size)
            .ExecuteUpdateAsync(set => set.SetProperty(s => s.Quantity, s => s.Quantity + quantity));

    public async Task<bool> SlugExistsAsync(string slug, Guid? excludeProductId = null) =>
        await DbSet.AnyAsync(p => p.Slug == slug && (excludeProductId == null || p.Id != excludeProductId));

    public async Task<bool> HasOrdersAsync(Guid productId) =>
        await Context.OrderItems.AnyAsync(oi => oi.ProductId == productId);
}
