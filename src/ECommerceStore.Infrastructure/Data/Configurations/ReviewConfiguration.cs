using ECommerceStore.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ECommerceStore.Infrastructure.Data.Configurations;

public class ReviewConfiguration : IEntityTypeConfiguration<Review>
{
    public void Configure(EntityTypeBuilder<Review> builder)
    {
        builder.ToTable("Reviews");

        builder.HasKey(r => r.Id);

        builder.Property(r => r.Id)
            .ValueGeneratedOnAdd();

        builder.Property(r => r.Rating)
            .IsRequired();

        builder.Property(r => r.Comment)
            .HasMaxLength(Review.MaxCommentLength);

        builder.Property(r => r.AuthorName)
            .IsRequired()
            .HasMaxLength(150);

        builder.Property(r => r.IsHidden)
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(r => r.CreatedAt)
            .IsRequired()
            .HasDefaultValueSql("timezone('utc', now())");

        // One review per customer account per product (the database enforces it even if two requests race).
        builder.HasIndex(r => new { r.ProductId, r.CustomerId })
            .IsUnique();

        builder.HasOne(r => r.Product)
            .WithMany(p => p.Reviews)
            .HasForeignKey(r => r.ProductId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(r => r.Customer)
            .WithMany()
            .HasForeignKey(r => r.CustomerId)
            .OnDelete(DeleteBehavior.Restrict);

        // The computed display name is not a column.
        builder.Ignore(r => r.PublicName);
    }
}
