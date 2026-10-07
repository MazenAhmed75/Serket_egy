using ECommerceStore.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ECommerceStore.Infrastructure.Data.Configurations;

public class PromoRedemptionConfiguration : IEntityTypeConfiguration<PromoRedemption>
{
    public void Configure(EntityTypeBuilder<PromoRedemption> builder)
    {
        builder.ToTable("PromoRedemptions");

        builder.HasKey(r => r.Id);

        builder.Property(r => r.Id)
            .ValueGeneratedOnAdd();

        builder.Property(r => r.RedeemedAt)
            .IsRequired()
            .HasDefaultValueSql("timezone('utc', now())");

        // One use per customer account per code — the database enforces it even if two requests race.
        builder.HasIndex(r => new { r.PromoCodeId, r.CustomerId })
            .IsUnique();

        builder.HasOne(r => r.PromoCode)
            .WithMany(p => p.Redemptions)
            .HasForeignKey(r => r.PromoCodeId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(r => r.Customer)
            .WithMany()
            .HasForeignKey(r => r.CustomerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(r => r.Order)
            .WithMany()
            .HasForeignKey(r => r.OrderId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
