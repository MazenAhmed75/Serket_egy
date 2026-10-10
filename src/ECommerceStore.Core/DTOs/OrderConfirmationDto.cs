using ECommerceStore.Core.Enums;

namespace ECommerceStore.Core.DTOs;

public class OrderConfirmationDto
{
    public string OrderNumber { get; set; } = string.Empty;

    public OrderStatus OrderStatus { get; set; }

    public PaymentMethod PaymentMethod { get; set; }

    public decimal SubtotalAmount { get; set; }

    public decimal DiscountAmount { get; set; }

    public decimal ShippingAmount { get; set; }

    public string? PromoCodeText { get; set; }

    public decimal TotalAmount { get; set; }

    public IReadOnlyList<OrderConfirmationItem> Items { get; set; } = Array.Empty<OrderConfirmationItem>();

    /// <summary>The address on one line (building and street, area, governorate).</summary>
    public string AddressLine { get; set; } = string.Empty;

    /// <summary>Optional extra details; empty when the customer left it blank.</summary>
    public string AddressDetail { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }
}

/// <summary>One item of a placed order, as shown on the confirmation page.</summary>
public sealed record OrderConfirmationItem(string ProductName, string? ColorName, string? Gender, string? Size, int Quantity, decimal UnitPrice);
