using ECommerceStore.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ECommerceStore.Infrastructure.Data.Configurations;

public class StockReminderConfiguration : IEntityTypeConfiguration<StockReminder>
{
    public void Configure(EntityTypeBuilder<StockReminder> builder)
    {
        builder.ToTable("StockReminders");

        builder.HasKey(r => r.Id);

        builder.Property(r => r.Id)
            .ValueGeneratedOnAdd();

        builder.Property(r => r.Size)
            .IsRequired()
            .HasMaxLength(10);

        builder.Property(r => r.Email)
            .IsRequired()
            .HasMaxLength(254);

        builder.Property(r => r.CreatedAt)
            .IsRequired()
            .HasDefaultValueSql("timezone('utc', now())");

        builder.HasIndex(r => new { r.ProductId, r.ProductColorId, r.Size });

        builder.HasOne(r => r.Product)
            .WithMany()
            .HasForeignKey(r => r.ProductId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(r => r.ProductColor)
            .WithMany()
            .HasForeignKey(r => r.ProductColorId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
