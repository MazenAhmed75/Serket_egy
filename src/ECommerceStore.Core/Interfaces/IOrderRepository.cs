using ECommerceStore.Core.Entities;

namespace ECommerceStore.Core.Interfaces;

public interface IOrderRepository : IRepository<Order>
{
    Task<Order?> GetByOrderNumberAsync(string orderNumber);

    /// <summary>Loads an order together with Customer, OrderItems (+ Product) and PaymentReceipts.</summary>
    Task<Order?> GetWithDetailsAsync(Guid orderId);

    /// <summary>
    /// Every order with Customer/OrderItems/PaymentReceipts loaded, newest first — used by the Admin dashboard and order list.
    /// When <paramref name="orderNumberContains"/> is given, only orders whose number contains it (ignoring case) are returned.
    /// </summary>
    Task<IReadOnlyList<Order>> GetAllWithDetailsAsync(string? orderNumberContains = null);
}
