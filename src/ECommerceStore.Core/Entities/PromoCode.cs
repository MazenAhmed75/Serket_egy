namespace ECommerceStore.Core.Entities;

/// <summary>
/// A promo code the owner creates. It can take a percentage off the items, give free shipping,
/// or both. Each customer account can use a given code once (see <see cref="PromoRedemption"/>).
/// </summary>
public class PromoCode
{
    public Guid Id { get; set; }

    /// <summary>Stored upper-case; matching is case-insensitive.</summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>1-100 when the code gives a percentage discount; null when it does not.</summary>
    public int? PercentOff { get; set; }

    public bool FreeShipping { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; }

    // Navigation
    public ICollection<PromoRedemption> Redemptions { get; set; } = new List<PromoRedemption>();
}
