using ECommerceStore.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ECommerceStore.Infrastructure.Data.Configurations;

public class OrderItemConfiguration : IEntityTypeConfiguration<OrderItem>
{
    public void Configure(EntityTypeBuilder<OrderItem> builder)
    {
        builder.ToTable("OrderItems");

        builder.HasKey(oi => oi.Id);

        builder.Property(oi => oi.Id)
            .ValueGeneratedOnAdd();

        builder.Property(oi => oi.Quantity)
            .IsRequired();

        builder.Property(oi => oi.UnitPrice)
            .IsRequired()
            .HasColumnType("decimal(18,2)");

        builder.Property(oi => oi.ColorName)
            .HasMaxLength(100);

        builder.Property(oi => oi.Gender)
            .HasMaxLength(20);

        builder.Property(oi => oi.Size)
            .HasMaxLength(20);

        builder.HasOne(oi => oi.Product)
            .WithMany(p => p.OrderItems)
            .HasForeignKey(oi => oi.ProductId)
            .OnDelete(DeleteBehavior.Restrict);

        // SetNull (not Cascade/Restrict): deleting a colour option must never delete or block
        // deleting historical order items — ColorName above already preserves what was ordered.
        builder.HasOne(oi => oi.ProductColor)
            .WithMany()
            .HasForeignKey(oi => oi.ProductColorId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
