using ECommerceStore.Core.Entities;
using Microsoft.EntityFrameworkCore;

namespace ECommerceStore.Infrastructure.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<Product> Products => Set<Product>();

    public DbSet<Customer> Customers => Set<Customer>();

    public DbSet<Order> Orders => Set<Order>();

    public DbSet<OrderItem> OrderItems => Set<OrderItem>();

    public DbSet<PaymentReceipt> PaymentReceipts => Set<PaymentReceipt>();

    public DbSet<ProductColor> ProductColors => Set<ProductColor>();

    public DbSet<ProductImage> ProductImages => Set<ProductImage>();

    public DbSet<ProductStock> ProductStocks => Set<ProductStock>();

    public DbSet<PromoCode> PromoCodes => Set<PromoCode>();

    public DbSet<PromoRedemption> PromoRedemptions => Set<PromoRedemption>();

    public DbSet<StoreSettings> StoreSettings => Set<StoreSettings>();

    public DbSet<Review> Reviews => Set<Review>();

    public DbSet<StockReminder> StockReminders => Set<StockReminder>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Picks up every IEntityTypeConfiguration<T> in this assembly
        // (ProductConfiguration, CustomerConfiguration, OrderConfiguration, etc.)
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }
}
