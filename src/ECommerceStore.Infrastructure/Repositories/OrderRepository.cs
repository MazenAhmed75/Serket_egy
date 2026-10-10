using ECommerceStore.Core.Entities;
using ECommerceStore.Core.Enums;
using ECommerceStore.Core.Interfaces;
using ECommerceStore.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ECommerceStore.Infrastructure.Repositories;

public class OrderRepository : Repository<Order>, IOrderRepository
{
    public OrderRepository(AppDbContext context) : base(context)
    {
    }

    public async Task<Order?> GetByOrderNumberAsync(string orderNumber) =>
        await DbSet
            .Include(o => o.Customer)
            .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Product)
            .Include(o => o.PaymentReceipts)
            .FirstOrDefaultAsync(o => o.OrderNumber == orderNumber);

    public async Task<Order?> GetWithDetailsAsync(Guid orderId) =>
        await DbSet
            .Include(o => o.Customer)
            .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Product)
            .Include(o => o.PaymentReceipts)
            .FirstOrDefaultAsync(o => o.Id == orderId);

    public async Task<IReadOnlyList<PaymentReceipt>> GetReceiptsWithImagesForFinishedOrdersAsync(DateTime createdBeforeUtc, int take) =>
        await Context.PaymentReceipts
            .Where(r => r.ImagePath != string.Empty
                        && r.Order.CreatedAt < createdBeforeUtc
                        && (r.Order.OrderStatus == OrderStatus.Shipped || r.Order.OrderStatus == OrderStatus.Cancelled))
            .OrderBy(r => r.UploadedAt)
            .Take(take)
            .ToListAsync();

    public async Task<IReadOnlyList<Order>> GetAllWithDetailsAsync(string? orderNumberContains = null)
    {
        var query = DbSet
            .Include(o => o.Customer)
            .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Product)
            .Include(o => o.PaymentReceipts)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(orderNumberContains))
        {
            // Order numbers are stored upper-case ("ORD-20261003-AB12CD"); a leading "#" is tolerated.
            var term = orderNumberContains.Trim().TrimStart('#').ToUpperInvariant();
            query = query.Where(o => o.OrderNumber.ToUpper().Contains(term));
        }

        return await query.OrderByDescending(o => o.CreatedAt).ToListAsync();
    }
}
