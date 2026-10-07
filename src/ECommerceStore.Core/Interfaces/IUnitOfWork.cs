namespace ECommerceStore.Core.Interfaces;

/// <summary>
/// Coordinates the entity-specific repositories against a single EF Core change-tracking context
/// so that a whole use case (e.g. "place order") commits as one transaction.
/// </summary>
public interface IUnitOfWork : IAsyncDisposable
{
    IProductRepository Products { get; }

    ICustomerRepository Customers { get; }

    IOrderRepository Orders { get; }

    IRepository<Entities.OrderItem> OrderItems { get; }

    IRepository<Entities.ProductColor> ProductColors { get; }

    IRepository<Entities.ProductImage> ProductImages { get; }

    IRepository<Entities.PaymentReceipt> PaymentReceipts { get; }

    IRepository<Entities.ProductStock> ProductStocks { get; }

    IPromoCodeRepository PromoCodes { get; }

    IRepository<Entities.PromoRedemption> PromoRedemptions { get; }

    IStoreSettingsRepository Settings { get; }

    IReviewRepository Reviews { get; }

    IStockReminderRepository StockReminders { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);

    /// <summary>Starts a transaction so several saves (and stock changes) succeed or fail as one.</summary>
    Task<ITransactionScope> BeginTransactionAsync(CancellationToken cancellationToken = default);
}
