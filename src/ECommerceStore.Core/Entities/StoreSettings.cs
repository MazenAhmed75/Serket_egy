namespace ECommerceStore.Core.Entities;

/// <summary>Single-row table of store-wide settings the owner can change from the Admin area.</summary>
public class StoreSettings
{
    public const decimal DefaultShippingFee = 50m;

    public const int DefaultReceiptRetentionDays = 60;

    public int Id { get; set; } = 1;

    /// <summary>Flat shipping fee in EGP added to every order (unless a promo code gives free shipping).</summary>
    public decimal ShippingFee { get; set; } = DefaultShippingFee;

    /// <summary>
    /// Receipt images of shipped or cancelled orders are deleted automatically once the order is this many days old.
    /// 0 keeps them forever. Orders still waiting for payment or shipping are never touched.
    /// </summary>
    public int ReceiptRetentionDays { get; set; } = DefaultReceiptRetentionDays;
}
