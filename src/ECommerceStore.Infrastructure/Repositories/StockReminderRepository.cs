using ECommerceStore.Core.Entities;
using ECommerceStore.Core.Interfaces;
using ECommerceStore.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ECommerceStore.Infrastructure.Repositories;

public class StockReminderRepository : Repository<StockReminder>, IStockReminderRepository
{
    public StockReminderRepository(AppDbContext context) : base(context)
    {
    }

    public async Task<bool> ExistsPendingAsync(Guid productId, Guid? productColorId, string size, string email) =>
        await DbSet.AnyAsync(r =>
            r.ProductId == productId &&
            r.ProductColorId == productColorId &&
            r.Size == size &&
            r.Email == email &&
            r.NotifiedAt == null);

    public async Task<IReadOnlyList<StockReminder>> GetPendingForProductAsync(Guid productId) =>
        await DbSet
            .Where(r => r.ProductId == productId && r.NotifiedAt == null)
            .ToListAsync();
}
