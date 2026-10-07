using ECommerceStore.Core.Entities;
using ECommerceStore.Core.Enums;
using ECommerceStore.Core.Interfaces;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ECommerceStore.Web.Areas.Admin.Pages.Orders;

public class IndexModel : PageModel
{
    private readonly IUnitOfWork _unitOfWork;

    public IndexModel(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public IReadOnlyList<Order> Orders { get; private set; } = Array.Empty<Order>();

    public OrderStatus? StatusFilter { get; private set; }

    /// <summary>The text typed in the search box (an order number or part of one).</summary>
    public string? Query { get; private set; }

    public async Task OnGetAsync(OrderStatus? status, string? q)
    {
        StatusFilter = status;
        Query = string.IsNullOrWhiteSpace(q) ? null : q.Trim();

        var all = await _unitOfWork.Orders.GetAllWithDetailsAsync(Query);

        Orders = (status.HasValue ? all.Where(o => o.OrderStatus == status.Value) : all).ToList();
    }
}
