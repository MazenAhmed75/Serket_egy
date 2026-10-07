using ECommerceStore.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ECommerceStore.Infrastructure.Data.Configurations;

public class StoreSettingsConfiguration : IEntityTypeConfiguration<StoreSettings>
{
    public void Configure(EntityTypeBuilder<StoreSettings> builder)
    {
        builder.ToTable("StoreSettings");

        builder.HasKey(s => s.Id);

        // The table only ever holds the single row with Id = 1.
        builder.Property(s => s.Id)
            .ValueGeneratedNever();

        builder.Property(s => s.ShippingFee)
            .IsRequired()
            .HasColumnType("decimal(18,2)");
    }
}
