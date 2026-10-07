using ECommerceStore.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ECommerceStore.Infrastructure.Data.Configurations;

public class ProductStockConfiguration : IEntityTypeConfiguration<ProductStock>
{
    public void Configure(EntityTypeBuilder<ProductStock> builder)
    {
        builder.ToTable("ProductStocks");

        builder.HasKey(s => s.Id);

        builder.Property(s => s.Id)
            .ValueGeneratedOnAdd();

        builder.Property(s => s.Size)
            .IsRequired()
            .HasMaxLength(10);

        builder.Property(s => s.Quantity)
            .IsRequired()
            .HasDefaultValue(0);

        builder.HasIndex(s => new { s.ProductId, s.ProductColorId, s.Size });

        builder.HasOne(s => s.Product)
            .WithMany(p => p.Stock)
            .HasForeignKey(s => s.ProductId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(s => s.ProductColor)
            .WithMany()
            .HasForeignKey(s => s.ProductColorId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
