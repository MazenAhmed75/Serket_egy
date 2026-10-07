using ECommerceStore.Core.Entities;
using ECommerceStore.Core.Enums;

namespace ECommerceStore.Core.Services;

/// <summary>Which status changes the shop owner may make, so a crafted request can't move an order somewhere it shouldn't go.</summary>
public static class OrderWorkflow
{
    /// <summary>Only an order waiting for its payment to be checked can be marked as paid.</summary>
    public static bool CanVerify(Order order) => order.OrderStatus == OrderStatus.PendingVerification;

    public static bool CanShip(Order order) => order.OrderStatus is OrderStatus.PendingPayment or OrderStatus.Paid;

    /// <summary>A shipped or already cancelled order can't be cancelled.</summary>
    public static bool CanCancel(Order order) => order.OrderStatus is not (OrderStatus.Shipped or OrderStatus.Cancelled);
}
