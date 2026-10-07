using ECommerceStore.Core.Entities;
using ECommerceStore.Core.Interfaces;
using ECommerceStore.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ECommerceStore.Infrastructure.Repositories;

public class PromoCodeRepository : Repository<PromoCode>, IPromoCodeRepository
{
    public PromoCodeRepository(AppDbContext context) : base(context)
    {
    }

    public async Task<PromoCode?> GetByCodeAsync(string code)
    {
        // Codes are stored upper-case, so an upper-cased lookup is case-insensitive.
        var normalized = code.Trim().ToUpperInvariant();
        return await DbSet.FirstOrDefaultAsync(p => p.Code == normalized);
    }

    public async Task<bool> HasRedeemedAsync(Guid promoCodeId, Guid customerId) =>
        await Context.PromoRedemptions.AnyAsync(r => r.PromoCodeId == promoCodeId && r.CustomerId == customerId);

    public async Task<int> CountRedemptionsAsync(Guid promoCodeId) =>
        await Context.PromoRedemptions.CountAsync(r => r.PromoCodeId == promoCodeId);

    public async Task<IReadOnlyList<PromoCode>> GetAllOrderedAsync() =>
        await DbSet.OrderByDescending(p => p.CreatedAt).ToListAsync();
}
