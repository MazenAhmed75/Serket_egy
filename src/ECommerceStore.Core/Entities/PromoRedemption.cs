namespace ECommerceStore.Core.Entities;

/// <summary>Records that a customer account used a promo code on an order (one row per code per customer).</summary>
public class PromoRedemption
{
    public Guid Id { get; set; }

    public Guid PromoCodeId { get; set; }

    public Guid CustomerId { get; set; }

    public Guid OrderId { get; set; }

    public DateTime RedeemedAt { get; set; }

    // Navigation
    public PromoCode PromoCode { get; set; } = null!;

    public Customer Customer { get; set; } = null!;

    public Order Order { get; set; } = null!;
}
