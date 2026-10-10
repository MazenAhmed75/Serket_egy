using ECommerceStore.Core.Entities;
using ECommerceStore.Core.Enums;

namespace ECommerceStore.Core.Services;

/// <summary>Where the order is delivered, as typed by the customer.</summary>
public sealed record OrderDelivery(
    string Governorate,
    string Area,
    string Street,
    string BuildingNumber,
    string AddressDetail,
    double Latitude,
    double Longitude);

/// <summary>Builds an <see cref="Order"/> from a validated cart (kept out of the page so it is simple to check).</summary>
public static class OrderBuilder
{
    public static string NewOrderNumber() =>
        $"ORD-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString("N")[..6].ToUpperInvariant()}";

    /// <summary>
    /// One order item per cart line, priced at the price the database says today. Only product ids are set on the
    /// items (never the product objects), so saving the order can never try to re-insert a product.
    /// </summary>
    public static Order Build(
        CartSummary summary,
        Customer customer,
        PriceBreakdown pricing,
        PaymentMethod paymentMethod,
        OrderDelivery delivery)
    {
        var requiresReceipt = paymentMethod != PaymentMethod.CashOnDelivery;

        return new Order
        {
            Id = Guid.NewGuid(),
            OrderNumber = NewOrderNumber(),
            CustomerId = customer.Id,
            Customer = customer,
            SubtotalAmount = pricing.Subtotal,
            DiscountAmount = pricing.Discount,
            ShippingAmount = pricing.Shipping,
            PromoCodeText = pricing.PromoCode,
            TotalAmount = pricing.Total,
            PaymentMethod = paymentMethod,
            OrderStatus = requiresReceipt ? OrderStatus.PendingVerification : OrderStatus.PendingPayment,
            Latitude = delivery.Latitude,
            Longitude = delivery.Longitude,
            Governorate = delivery.Governorate,
            Area = delivery.Area.Trim(),
            Street = delivery.Street.Trim(),
            BuildingNumber = delivery.BuildingNumber.Trim(),
            AddressDetail = delivery.AddressDetail.Trim(),
            CreatedAt = DateTime.UtcNow,
            OrderItems = summary.Items.Select(item => new OrderItem
            {
                Id = Guid.NewGuid(),
                ProductId = item.Line.ProductId,
                Quantity = item.Line.Quantity,
                UnitPrice = item.UnitPrice,
                ProductColorId = item.Line.ColorId,
                ColorName = item.ColorName,
                Gender = item.Line.Gender,
                Size = item.Line.Size
            }).ToList()
        };
    }
}
