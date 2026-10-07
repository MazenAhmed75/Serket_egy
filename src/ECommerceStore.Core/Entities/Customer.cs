namespace ECommerceStore.Core.Entities;

public class Customer
{
    public Guid Id { get; set; }

    public string FullName { get; set; } = string.Empty;

    public string PhoneNumber { get; set; } = string.Empty;

    public string? Email { get; set; }

    /// <summary>Null for guest customers (checked out without an account); set once they register.</summary>
    public string? PasswordHash { get; set; }

    // Navigation
    public ICollection<Order> Orders { get; set; } = new List<Order>();
}
