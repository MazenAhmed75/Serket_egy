using ECommerceStore.Core.Entities;
using ECommerceStore.Core.Interfaces;
using ECommerceStore.Infrastructure.Data;

namespace ECommerceStore.Infrastructure.Repositories;

public class UnitOfWork : IUnitOfWork
{
    private readonly AppDbContext _context;

    public UnitOfWork(AppDbContext context)
    {
        _context = context;
        Products = new ProductRepository(_context);
        Customers = new CustomerRepository(_context);
        Orders = new OrderRepository(_context);
        OrderItems = new Repository<OrderItem>(_context);
        ProductColors = new Repository<ProductColor>(_context);
        ProductImages = new Repository<ProductImage>(_context);
        PaymentReceipts = new Repository<PaymentReceipt>(_context);
        ProductStocks = new Repository<ProductStock>(_context);
        PromoCodes = new PromoCodeRepository(_context);
        PromoRedemptions = new Repository<PromoRedemption>(_context);
        Settings = new StoreSettingsRepository(_context);
        Reviews = new ReviewRepository(_context);
        StockReminders = new StockReminderRepository(_context);
    }

    public IProductRepository Products { get; }

    public ICustomerRepository Customers { get; }

    public IOrderRepository Orders { get; }

    public IRepository<OrderItem> OrderItems { get; }

    public IRepository<ProductColor> ProductColors { get; }

    public IRepository<ProductImage> ProductImages { get; }

    public IRepository<PaymentReceipt> PaymentReceipts { get; }

    public IRepository<ProductStock> ProductStocks { get; }

    public IPromoCodeRepository PromoCodes { get; }

    public IRepository<PromoRedemption> PromoRedemptions { get; }

    public IStoreSettingsRepository Settings { get; }

    public IReviewRepository Reviews { get; }

    public IStockReminderRepository StockReminders { get; }

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) =>
        _context.SaveChangesAsync(cancellationToken);

    public async Task<ITransactionScope> BeginTransactionAsync(CancellationToken cancellationToken = default) =>
        new DbTransactionScope(await _context.Database.BeginTransactionAsync(cancellationToken));

    public async ValueTask DisposeAsync()
    {
        await _context.DisposeAsync();
        GC.SuppressFinalize(this);
    }
}

/// <summary>Adapts EF Core's transaction to the Core layer's <see cref="ITransactionScope"/>.</summary>
internal sealed class DbTransactionScope : ITransactionScope
{
    private readonly Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction _transaction;

    public DbTransactionScope(Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction transaction)
    {
        _transaction = transaction;
    }

    public Task CommitAsync(CancellationToken cancellationToken = default) => _transaction.CommitAsync(cancellationToken);

    public Task RollbackAsync(CancellationToken cancellationToken = default) => _transaction.RollbackAsync(cancellationToken);

    // Disposing a transaction that was not committed rolls it back.
    public ValueTask DisposeAsync() => _transaction.DisposeAsync();
}
