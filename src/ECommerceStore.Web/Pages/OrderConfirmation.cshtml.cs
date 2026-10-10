using ECommerceStore.Core.DTOs;
using ECommerceStore.Core.Interfaces;
using ECommerceStore.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ECommerceStore.Web.Pages;

public class OrderConfirmationModel : PageModel
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly OrderConfirmationLinks _links;

    public OrderConfirmationModel(IUnitOfWork unitOfWork, OrderConfirmationLinks links)
    {
        _unitOfWork = unitOfWork;
        _links = links;
    }

    public OrderConfirmationDto? Order { get; private set; }

    public async Task<IActionResult> OnGetAsync(string? t)
    {
        var orderNumber = _links.ReadToken(t);
        if (orderNumber is null)
        {
            // No (or an invalid/expired) link: send the visitor home instead of revealing whether an order exists.
            return RedirectToPage("/Index");
        }

        var order = await _unitOfWork.Orders.GetByOrderNumberAsync(orderNumber);

        if (order is null)
        {
            return NotFound();
        }

        Order = new OrderConfirmationDto
        {
            OrderNumber = order.OrderNumber,
            OrderStatus = order.OrderStatus,
            PaymentMethod = order.PaymentMethod,
            SubtotalAmount = order.SubtotalAmount,
            DiscountAmount = order.DiscountAmount,
            ShippingAmount = order.ShippingAmount,
            PromoCodeText = order.PromoCodeText,
            TotalAmount = order.TotalAmount,
            Items = order.OrderItems
                .Select(i => new OrderConfirmationItem(i.Product?.Name ?? string.Empty, i.ColorName, i.Gender, i.Size, i.Quantity, i.UnitPrice))
                .ToList(),
            AddressLine = order.AddressLine,
            AddressDetail = order.AddressDetail,
            CreatedAt = order.CreatedAt
        };

        return Page();
    }
}
