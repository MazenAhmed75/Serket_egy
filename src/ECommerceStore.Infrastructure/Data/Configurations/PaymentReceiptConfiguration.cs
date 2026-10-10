using ECommerceStore.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ECommerceStore.Infrastructure.Data.Configurations;

public class PaymentReceiptConfiguration : IEntityTypeConfiguration<PaymentReceipt>
{
    public void Configure(EntityTypeBuilder<PaymentReceipt> builder)
    {
        builder.ToTable("PaymentReceipts");

        builder.HasKey(pr => pr.Id);

        builder.Property(pr => pr.Id)
            .ValueGeneratedOnAdd();

        builder.Property(pr => pr.ImagePath)
            .IsRequired()
            .HasMaxLength(500);

        // Computed from ImagePath; not a column.
        builder.Ignore(pr => pr.HasImage);

        builder.Property(pr => pr.TransactionReference)
            .HasMaxLength(100);

        builder.Property(pr => pr.UploadedAt)
            .IsRequired()
            .HasDefaultValueSql("timezone('utc', now())");

        builder.Property(pr => pr.IsVerified)
            .IsRequired()
            .HasDefaultValue(false);

        builder.HasOne(pr => pr.Order)
            .WithMany(o => o.PaymentReceipts)
            .HasForeignKey(pr => pr.OrderId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
