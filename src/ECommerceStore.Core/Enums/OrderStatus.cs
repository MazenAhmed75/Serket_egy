namespace ECommerceStore.Core.Enums;

/// <summary>
/// Lifecycle states for an <see cref="Entities.Order"/>.
/// </summary>
public enum OrderStatus
{
    /// <summary>Order created, awaiting the customer to submit payment.</summary>
    PendingPayment = 0,

    /// <summary>Customer submitted a receipt/reference; awaiting admin verification.</summary>
    PendingVerification = 1,

    /// <summary>Payment verified by the admin.</summary>
    Paid = 2,

    /// <summary>Order has been shipped/handed over for delivery.</summary>
    Shipped = 3,

    /// <summary>Order was cancelled.</summary>
    Cancelled = 4
}
