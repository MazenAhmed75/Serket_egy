using ECommerceStore.Core.Entities;

namespace ECommerceStore.Core.Services;

/// <summary>The three money lines shown at checkout (plus the optional discount line).</summary>
public sealed record PriceBreakdown(
    decimal Subtotal,
    decimal Discount,
    decimal Shipping,
    decimal Total,
    int PercentOff,
    bool FreeShipping,
    string? PromoCode);

public static class PricingCalculator
{
    /// <summary>
    /// Items subtotal, minus the promo's percentage (if any), plus shipping (0 when the promo gives free shipping).
    /// </summary>
    public static PriceBreakdown Calculate(decimal unitPrice, int quantity, decimal shippingFee, PromoCode? promo)
    {
        var subtotal = unitPrice * quantity;

        var percent = promo?.PercentOff is > 0 and <= 100 ? promo.PercentOff!.Value : 0;
        var discount = percent > 0
            ? Math.Round(subtotal * percent / 100m, 2, MidpointRounding.AwayFromZero)
            : 0m;

        var freeShipping = promo?.FreeShipping == true;
        var shipping = freeShipping ? 0m : shippingFee;

        return new PriceBreakdown(subtotal, discount, shipping, subtotal - discount + shipping, percent, freeShipping, promo?.Code);
    }
}
