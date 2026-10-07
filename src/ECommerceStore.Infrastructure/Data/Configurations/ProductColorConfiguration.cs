using ECommerceStore.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ECommerceStore.Infrastructure.Data.Configurations;

public class ProductColorConfiguration : IEntityTypeConfiguration<ProductColor>
{
    public void Configure(EntityTypeBuilder<ProductColor> builder)
    {
        builder.ToTable("ProductColors");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.Id)
            .ValueGeneratedOnAdd();

        builder.Property(c => c.Name)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(c => c.HexCode)
            .IsRequired()
            .HasMaxLength(20);

        builder.Property(c => c.ImageUrl)
            .HasMaxLength(500);

        builder.Property(c => c.IsActive)
            .IsRequired()
            .HasDefaultValue(true);

        builder.HasOne(c => c.Product)
            .WithMany(p => p.Colors)
            .HasForeignKey(c => c.ProductId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
