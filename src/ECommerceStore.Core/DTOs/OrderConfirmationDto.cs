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

    public int Quantity { get; set; }

    public string ProductName { get; set; } = string.Empty;

    public string? ColorName { get; set; }

    public string? Gender { get; set; }

    public string? Size { get; set; }

    /// <summary>The address on one line (building and street, area, governorate).</summary>
    public string AddressLine { get; set; } = string.Empty;

    /// <summary>Optional extra details; empty when the customer left it blank.</summary>
    public string AddressDetail { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }
}
