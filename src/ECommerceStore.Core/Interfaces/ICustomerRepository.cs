using ECommerceStore.Core.Entities;

namespace ECommerceStore.Core.Interfaces;

public interface ICustomerRepository : IRepository<Customer>
{
    /// <summary>A registered account (one with a password) that uses this phone number.</summary>
    Task<Customer?> GetRegisteredByPhoneNumberAsync(string phoneNumber);

    /// <summary>Finds a registered account (one with a password) by e-mail, case-insensitively.</summary>
    Task<Customer?> GetRegisteredByEmailAsync(string email);
}
