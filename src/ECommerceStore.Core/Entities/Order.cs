using ECommerceStore.Core.Enums;

namespace ECommerceStore.Core.Entities;

public class Order
{
    public Guid Id { get; set; }

    /// <summary>Human-friendly, unique order reference shown to the customer (e.g. "ORD-20260928-0001").</summary>
    public string OrderNumber { get; set; } = string.Empty;

    public Guid CustomerId { get; set; }

    /// <summary>Price of the items before discount and shipping.</summary>
    public decimal SubtotalAmount { get; set; }

    /// <summary>Amount taken off the items by a percentage promo code (0 when none).</summary>
    public decimal DiscountAmount { get; set; }

    /// <summary>Shipping charged on this order (0 when a promo code gave free shipping).</summary>
    public decimal ShippingAmount { get; set; }

    /// <summary>The promo code used, copied as text so the order still shows it if the code is later removed.</summary>
    public string? PromoCodeText { get; set; }

    /// <summary>Items - discount + shipping.</summary>
    public decimal TotalAmount { get; set; }

    public OrderStatus OrderStatus { get; set; }

    public PaymentMethod PaymentMethod { get; set; }

    /// <summary>Delivery pin dropped by the customer via Leaflet.js / OpenStreetMap.</summary>
    public double Latitude { get; set; }

    public double Longitude { get; set; }

    /// <summary>One of <see cref="Constants.EgyptGovernorates.All"/>; empty on orders placed before governorates were collected.</summary>
    public string Governorate { get; set; } = string.Empty;

    /// <summary>The neighbourhood/area, typed by the customer.</summary>
    public string Area { get; set; } = string.Empty;

    /// <summary>The street name, typed by the customer.</summary>
    public string Street { get; set; } = string.Empty;

    /// <summary>The building number, typed by the customer.</summary>
    public string BuildingNumber { get; set; } = string.Empty;

    /// <summary>The address on one line, e.g. "12 Road 9, Maadi, Cairo" (parts that are empty are left out, so older orders still read well).</summary>
    public string AddressLine => string.Join(", ", new[]
    {
        $"{BuildingNumber} {Street}".Trim(),
        Area,
        Governorate
    }.Where(part => !string.IsNullOrWhiteSpace(part)));

    /// <summary>Optional extra delivery details (floor, apartment, landmark, etc.) to accompany the pin; empty when the customer left it blank.</summary>
    public string AddressDetail { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }

    // Navigation
    public Customer Customer { get; set; } = null!;

    public ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();

    public ICollection<PaymentReceipt> PaymentReceipts { get; set; } = new List<PaymentReceipt>();
}
