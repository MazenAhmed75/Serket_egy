using ECommerceStore.Core.DTOs;
using ECommerceStore.Core.Entities;
using ECommerceStore.Core.Interfaces;
using ECommerceStore.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ECommerceStore.Infrastructure.Repositories;

public class ReviewRepository : Repository<Review>, IReviewRepository
{
    public ReviewRepository(AppDbContext context) : base(context)
    {
    }

    public async Task<IReadOnlyList<Review>> GetVisibleForProductAsync(Guid productId) =>
        await DbSet
            .AsNoTracking()
            .Where(r => r.ProductId == productId && !r.IsHidden)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync();

    public async Task<IReadOnlyDictionary<Guid, RatingSummary>> GetSummariesAsync()
    {
        var groups = await DbSet
            .Where(r => !r.IsHidden)
            .GroupBy(r => r.ProductId)
            .Select(g => new { ProductId = g.Key, Average = g.Average(r => (double)r.Rating), Count = g.Count() })
            .ToListAsync();

        return groups.ToDictionary(g => g.ProductId, g => new RatingSummary(g.Average, g.Count));
    }

    public async Task<bool> HasReviewedAsync(Guid productId, Guid customerId) =>
        await DbSet.AnyAsync(r => r.ProductId == productId && r.CustomerId == customerId);

    public async Task<IReadOnlyList<Review>> GetAllWithProductAsync() =>
        await DbSet
            .Include(r => r.Product)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync();
}
