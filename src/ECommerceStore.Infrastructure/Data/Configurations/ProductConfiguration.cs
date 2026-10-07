using ECommerceStore.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ECommerceStore.Infrastructure.Data.Configurations;

public class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        builder.ToTable("Products");

        builder.HasKey(p => p.Id);

        builder.Property(p => p.Id)
            .ValueGeneratedOnAdd();

        builder.Property(p => p.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(p => p.Slug)
            .IsRequired()
            .HasMaxLength(120);

        builder.HasIndex(p => p.Slug)
            .IsUnique();

        builder.Property(p => p.ShortDescription)
            .HasMaxLength(200);

        builder.Property(p => p.SortOrder)
            .IsRequired()
            .HasDefaultValue(0);

        builder.Property(p => p.DiscountPercent)
            .IsRequired()
            .HasDefaultValue(0);

        builder.Property(p => p.DiscountActive)
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(p => p.Description)
            .IsRequired()
            .HasMaxLength(4000);

        builder.Property(p => p.Price)
            .IsRequired()
            .HasColumnType("decimal(18,2)");

        builder.Property(p => p.ImageUrl)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(p => p.IsActive)
            .IsRequired()
            .HasDefaultValue(true);

        builder.Property(p => p.CreatedAt)
            .IsRequired()
            .HasDefaultValueSql("timezone('utc', now())");

        builder.HasIndex(p => p.IsActive);
    }
}
