using ECommerceStore.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ECommerceStore.Infrastructure.Data.Configurations;

public class OrderConfiguration : IEntityTypeConfiguration<Order>
{
    public void Configure(EntityTypeBuilder<Order> builder)
    {
        builder.ToTable("Orders");

        builder.HasKey(o => o.Id);

        builder.Property(o => o.Id)
            .ValueGeneratedOnAdd();

        builder.Property(o => o.OrderNumber)
            .IsRequired()
            .HasMaxLength(50);

        builder.HasIndex(o => o.OrderNumber)
            .IsUnique();

        builder.Property(o => o.SubtotalAmount)
            .IsRequired()
            .HasColumnType("decimal(18,2)")
            .HasDefaultValue(0m);

        builder.Property(o => o.DiscountAmount)
            .IsRequired()
            .HasColumnType("decimal(18,2)")
            .HasDefaultValue(0m);

        builder.Property(o => o.ShippingAmount)
            .IsRequired()
            .HasColumnType("decimal(18,2)")
            .HasDefaultValue(0m);

        builder.Property(o => o.PromoCodeText)
            .HasMaxLength(50);

        builder.Property(o => o.Governorate)
            .IsRequired()
            .HasMaxLength(50)
            .HasDefaultValue(string.Empty);

        builder.Property(o => o.Area)
            .IsRequired()
            .HasMaxLength(100)
            .HasDefaultValue(string.Empty);

        builder.Property(o => o.Street)
            .IsRequired()
            .HasMaxLength(150)
            .HasDefaultValue(string.Empty);

        builder.Property(o => o.BuildingNumber)
            .IsRequired()
            .HasMaxLength(30)
            .HasDefaultValue(string.Empty);

        // The one-line address is built from the parts above and is not a column.
        builder.Ignore(o => o.AddressLine);

        builder.Property(o => o.TotalAmount)
            .IsRequired()
            .HasColumnType("decimal(18,2)");

        builder.Property(o => o.OrderStatus)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(30);

        builder.Property(o => o.PaymentMethod)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(30);

        builder.Property(o => o.Latitude)
            .IsRequired()
            .HasColumnType("double precision");

        builder.Property(o => o.Longitude)
            .IsRequired()
            .HasColumnType("double precision");

        builder.Property(o => o.AddressDetail)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(o => o.CreatedAt)
            .IsRequired()
            .HasDefaultValueSql("timezone('utc', now())");

        builder.HasIndex(o => o.OrderStatus);

        builder.HasOne(o => o.Customer)
            .WithMany(c => c.Orders)
            .HasForeignKey(o => o.CustomerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(o => o.OrderItems)
            .WithOne(oi => oi.Order)
            .HasForeignKey(oi => oi.OrderId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(o => o.PaymentReceipts)
            .WithOne(pr => pr.Order)
            .HasForeignKey(pr => pr.OrderId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
