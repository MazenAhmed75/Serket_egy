using ECommerceStore.Core.Entities;
using ECommerceStore.Core.Interfaces;
using ECommerceStore.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ECommerceStore.Infrastructure.Repositories;

public class CustomerRepository : Repository<Customer>, ICustomerRepository
{
    public CustomerRepository(AppDbContext context) : base(context)
    {
    }

    public async Task<Customer?> GetRegisteredByPhoneNumberAsync(string phoneNumber) =>
        await DbSet.FirstOrDefaultAsync(c => c.PhoneNumber == phoneNumber && c.PasswordHash != null);

    public async Task<Customer?> GetRegisteredByEmailAsync(string email)
    {
        var normalized = email.Trim().ToLowerInvariant();
        return await DbSet.FirstOrDefaultAsync(c => c.PasswordHash != null && c.Email != null && c.Email.ToLower() == normalized);
    }
}
