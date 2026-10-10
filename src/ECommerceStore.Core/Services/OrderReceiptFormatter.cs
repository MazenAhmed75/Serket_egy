using System.Globalization;
using System.Text;
using ECommerceStore.Core.Entities;
using ECommerceStore.Core.Enums;

namespace ECommerceStore.Core.Services;

/// <summary>Builds the plain-text messages sent when an order is placed: the receipt for the customer and the alert for the owner.</summary>
public static class OrderReceiptFormatter
{
    public static string Subject(Order order) => $"Your Serket order {order.OrderNumber}";

    public static string OwnerSubject(Order order) => $"New order {order.OrderNumber}";

    /// <summary>The customer's receipt. The order must have its customer and order items set; <paramref name="productNames"/> maps product ids to names.</summary>
    public static string Body(Order order, IReadOnlyDictionary<Guid, string> productNames)
    {
        var firstName = order.Customer.FullName.Split(' ', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? "there";

        var text = new StringBuilder();
        text.AppendLine($"Hi {firstName},");
        text.AppendLine();
        text.AppendLine("Thank you for your order! Here is your receipt.");
        text.AppendLine();
        text.AppendLine($"Order number: {order.OrderNumber}");
        text.AppendLine($"Date: {order.CreatedAt:dd MMM yyyy} (UTC)");
        text.AppendLine($"Payment: {PaymentName(order.PaymentMethod)}");
        text.AppendLine();
        text.AppendLine(order.OrderItems.Count == 1 ? "Item" : "Items");
        AppendItems(text, order, productNames);
        text.AppendLine();
        AppendTotals(text, order);
        text.AppendLine();
        text.AppendLine("Delivery to");
        text.AppendLine($"  {order.AddressLine}");
        if (!string.IsNullOrWhiteSpace(order.AddressDetail))
        {
            text.AppendLine($"  {order.AddressDetail}");
        }

        text.AppendLine();
        text.AppendLine(order.PaymentMethod == PaymentMethod.CashOnDelivery
            ? "You pay in cash when your order arrives."
            : "We received your payment receipt and will check it before confirming your order.");
        text.AppendLine();
        text.AppendLine("Serket");

        return text.ToString();
    }

    /// <summary>The alert to the shop owner.</summary>
    public static string OwnerBody(Order order, IReadOnlyDictionary<Guid, string> productNames, string? customerPhone)
    {
        var lat = order.Latitude.ToString(CultureInfo.InvariantCulture);
        var lng = order.Longitude.ToString(CultureInfo.InvariantCulture);

        var text = new StringBuilder();
        text.AppendLine($"Customer: {order.Customer.FullName} ({customerPhone ?? order.Customer.PhoneNumber})");
        text.AppendLine(order.OrderItems.Count == 1 ? "Item:" : "Items:");
        AppendItems(text, order, productNames);
        AppendTotals(text, order);
        text.AppendLine($"Payment method: {PaymentName(order.PaymentMethod)}");
        text.AppendLine($"Delivery: {order.AddressLine}" + (string.IsNullOrWhiteSpace(order.AddressDetail) ? string.Empty : $" - {order.AddressDetail}"));
        text.AppendLine($"Map: https://www.openstreetmap.org/?mlat={lat}&mlon={lng}#map=17/{lat}/{lng}");
        text.AppendLine($"Order status: {order.OrderStatus}");
        return text.ToString();
    }

    private static void AppendItems(StringBuilder text, Order order, IReadOnlyDictionary<Guid, string> productNames)
    {
        foreach (var item in order.OrderItems)
        {
            var details = new List<string>();
            if (!string.IsNullOrEmpty(item.Gender)) details.Add(item.Gender == "Female" ? "Women's fit" : "Men's fit");
            if (!string.IsNullOrEmpty(item.Size)) details.Add($"Size {item.Size}");
            if (!string.IsNullOrEmpty(item.ColorName)) details.Add(item.ColorName);

            var name = productNames.TryGetValue(item.ProductId, out var productName) ? productName : "Product";
            text.AppendLine($"  {item.Quantity} x {name}" + (details.Count > 0 ? $" ({string.Join(", ", details)})" : string.Empty));
            text.AppendLine($"    {item.UnitPrice:N2} EGP each");
        }
    }

    private static void AppendTotals(StringBuilder text, Order order)
    {
        text.AppendLine($"Items:    {order.SubtotalAmount:N2} EGP");
        if (order.DiscountAmount > 0)
        {
            var code = string.IsNullOrEmpty(order.PromoCodeText) ? string.Empty : $" ({order.PromoCodeText})";
            text.AppendLine($"Discount: -{order.DiscountAmount:N2} EGP{code}");
        }

        text.AppendLine($"Shipping: {(order.ShippingAmount <= 0 ? "Free" : $"{order.ShippingAmount:N2} EGP")}");
        text.AppendLine($"Total:    {order.TotalAmount:N2} EGP");
    }

    private static string PaymentName(PaymentMethod method) => method switch
    {
        PaymentMethod.CashOnDelivery => "Cash on delivery",
        PaymentMethod.VodafoneCash => "Vodafone Cash",
        PaymentMethod.InstaPay => "InstaPay",
        _ => method.ToString()
    };
}
