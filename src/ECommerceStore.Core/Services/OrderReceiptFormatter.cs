using System.Text;
using ECommerceStore.Core.Entities;
using ECommerceStore.Core.Enums;

namespace ECommerceStore.Core.Services;

/// <summary>Builds the plain-text receipt e-mailed to the customer when an order is placed.</summary>
public static class OrderReceiptFormatter
{
    public static string Subject(Order order) => $"Your Serket order {order.OrderNumber}";

    /// <summary>The order must have its customer and order items set.</summary>
    public static string Body(Order order, string productName)
    {
        var firstName = order.Customer.FullName.Split(' ', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? "there";
        var item = order.OrderItems.First();

        var details = new List<string>();
        if (!string.IsNullOrEmpty(item.Gender)) details.Add(item.Gender == "Female" ? "Women's fit" : "Men's fit");
        if (!string.IsNullOrEmpty(item.Size)) details.Add($"Size {item.Size}");
        if (!string.IsNullOrEmpty(item.ColorName)) details.Add(item.ColorName);

        var text = new StringBuilder();
        text.AppendLine($"Hi {firstName},");
        text.AppendLine();
        text.AppendLine("Thank you for your order! Here is your receipt.");
        text.AppendLine();
        text.AppendLine($"Order number: {order.OrderNumber}");
        text.AppendLine($"Date: {order.CreatedAt:dd MMM yyyy} (UTC)");
        text.AppendLine($"Payment: {PaymentName(order.PaymentMethod)}");
        text.AppendLine();
        text.AppendLine("Item");
        text.AppendLine($"  {item.Quantity} x {productName}" + (details.Count > 0 ? $" ({string.Join(", ", details)})" : string.Empty));
        text.AppendLine($"  {item.UnitPrice:N2} EGP each");
        text.AppendLine();
        text.AppendLine($"Items:    {order.SubtotalAmount:N2} EGP");
        if (order.DiscountAmount > 0)
        {
            var code = string.IsNullOrEmpty(order.PromoCodeText) ? string.Empty : $" ({order.PromoCodeText})";
            text.AppendLine($"Discount: -{order.DiscountAmount:N2} EGP{code}");
        }

        text.AppendLine($"Shipping: {(order.ShippingAmount <= 0 ? "Free" : $"{order.ShippingAmount:N2} EGP")}");
        text.AppendLine($"Total:    {order.TotalAmount:N2} EGP");
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

    private static string PaymentName(PaymentMethod method) => method switch
    {
        PaymentMethod.CashOnDelivery => "Cash on delivery",
        PaymentMethod.VodafoneCash => "Vodafone Cash",
        PaymentMethod.InstaPay => "InstaPay",
        _ => method.ToString()
    };
}
