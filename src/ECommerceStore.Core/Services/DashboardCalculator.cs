using ECommerceStore.Core.Entities;
using ECommerceStore.Core.Enums;

namespace ECommerceStore.Core.Services;

/// <summary>One calendar month's figures (Month is always the first day of that month).</summary>
public sealed record MonthStats(DateOnly Month, int Orders, int Cancelled, decimal Revenue);

/// <summary>Everything the Admin dashboard shows for the chosen period.</summary>
public sealed record DashboardSummary(
    int TotalOrders,
    int CancelledOrders,
    decimal Revenue,
    IReadOnlyDictionary<OrderStatus, int> StatusCounts,
    IReadOnlyList<Order> RecentOrders,
    IReadOnlyList<MonthStats> Months);

/// <summary>The dashboard's numbers, worked out from the orders (kept out of the page so it is easy to check and reuse).</summary>
public static class DashboardCalculator
{
    private const int RecentOrderCount = 10;

    /// <summary>Money counts as revenue once an order is paid or shipped; cancelled and pending orders do not.</summary>
    public static bool CountsAsRevenue(Order order) => order.OrderStatus is OrderStatus.Paid or OrderStatus.Shipped;

    /// <summary>
    /// Summarises <paramref name="orders"/> (newest first). With <paramref name="month"/> set, only orders placed in that
    /// month are counted; the month-by-month table always covers every order. Months follow <paramref name="zone"/>
    /// (the server's local time zone by default, the same one the admin pages use to show order times).
    /// </summary>
    public static DashboardSummary Summarize(IReadOnlyList<Order> orders, DateOnly? month, TimeZoneInfo? zone = null)
    {
        zone ??= TimeZoneInfo.Local;

        var inPeriod = month is null
            ? orders
            : orders.Where(o => MonthOf(o, zone) == month.Value).ToList();

        var statusCounts = Enum.GetValues<OrderStatus>()
            .ToDictionary(status => status, status => inPeriod.Count(o => o.OrderStatus == status));

        var months = orders
            .GroupBy(o => MonthOf(o, zone))
            .Select(g => new MonthStats(
                g.Key,
                g.Count(),
                g.Count(o => o.OrderStatus == OrderStatus.Cancelled),
                g.Where(CountsAsRevenue).Sum(o => o.TotalAmount)))
            .OrderByDescending(m => m.Month)
            .ToList();

        return new DashboardSummary(
            inPeriod.Count,
            statusCounts[OrderStatus.Cancelled],
            inPeriod.Where(CountsAsRevenue).Sum(o => o.TotalAmount),
            statusCounts,
            inPeriod.Take(RecentOrderCount).ToList(),
            months);
    }

    /// <summary>The first day of the month the order was placed in, in the given time zone.</summary>
    public static DateOnly MonthOf(Order order, TimeZoneInfo zone)
    {
        var utc = DateTime.SpecifyKind(order.CreatedAt, DateTimeKind.Utc);
        var local = TimeZoneInfo.ConvertTimeFromUtc(utc, zone);
        return new DateOnly(local.Year, local.Month, 1);
    }
}
