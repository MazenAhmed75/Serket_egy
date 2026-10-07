using ECommerceStore.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ECommerceStore.Infrastructure.Data.Configurations;

public class CustomerConfiguration : IEntityTypeConfiguration<Customer>
{
    public void Configure(EntityTypeBuilder<Customer> builder)
    {
        builder.ToTable("Customers");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.Id)
            .ValueGeneratedOnAdd();

        builder.Property(c => c.FullName)
            .IsRequired()
            .HasMaxLength(150);

        builder.Property(c => c.PhoneNumber)
            .IsRequired()
            .HasMaxLength(20);

        builder.Property(c => c.Email)
            .HasMaxLength(200);

        builder.Property(c => c.PasswordHash)
            .HasMaxLength(300);

        builder.HasIndex(c => c.PhoneNumber);

        // Registered accounts sign in with their e-mail, so it must be unique among them.
        // Guest customers (no password) may share an e-mail, hence the filter.
        builder.HasIndex(c => c.Email)
            .IsUnique()
            .HasFilter("\"PasswordHash\" IS NOT NULL AND \"Email\" IS NOT NULL");

        builder.HasMany(c => c.Orders)
            .WithOne(o => o.Customer)
            .HasForeignKey(o => o.CustomerId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
