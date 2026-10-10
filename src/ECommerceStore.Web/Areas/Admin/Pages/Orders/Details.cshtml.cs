using ECommerceStore.Core.Entities;
using ECommerceStore.Core.Enums;
using ECommerceStore.Core.Exceptions;
using ECommerceStore.Core.Interfaces;
using ECommerceStore.Core.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ECommerceStore.Web.Areas.Admin.Pages.Orders;

public class DetailsModel : PageModel
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IFileStorageService _fileStorage;

    public DetailsModel(IUnitOfWork unitOfWork, IFileStorageService fileStorage)
    {
        _unitOfWork = unitOfWork;
        _fileStorage = fileStorage;
    }

    public Order? Order { get; private set; }

    [TempData]
    public string? StatusMessage { get; set; }

    public async Task<IActionResult> OnGetAsync(Guid id)
    {
        Order = await _unitOfWork.Orders.GetWithDetailsAsync(id);
        return Order is null ? NotFound() : Page();
    }

    public async Task<IActionResult> OnPostVerifyAsync(Guid id)
    {
        var order = await _unitOfWork.Orders.GetWithDetailsAsync(id);
        if (order is null)
        {
            return NotFound();
        }

        if (!OrderWorkflow.CanVerify(order))
        {
            StatusMessage = "This order is not waiting for payment verification.";
            return RedirectToPage(new { id });
        }

        order.OrderStatus = OrderStatus.Paid;
        foreach (var receipt in order.PaymentReceipts)
        {
            receipt.IsVerified = true;
        }

        _unitOfWork.Orders.Update(order);
        await _unitOfWork.SaveChangesAsync();

        StatusMessage = $"Order {order.OrderNumber} marked as paid.";
        return RedirectToPage(new { id });
    }

    public async Task<IActionResult> OnPostShipAsync(Guid id)
    {
        var order = await _unitOfWork.Orders.GetByIdAsync(id);
        if (order is null)
        {
            return NotFound();
        }

        if (!OrderWorkflow.CanShip(order))
        {
            StatusMessage = "This order can't be marked as shipped from its current status.";
            return RedirectToPage(new { id });
        }

        order.OrderStatus = OrderStatus.Shipped;
        _unitOfWork.Orders.Update(order);
        await _unitOfWork.SaveChangesAsync();

        StatusMessage = "Order marked as shipped.";
        return RedirectToPage(new { id });
    }

    /// <summary>Deletes the screenshot of one receipt to free storage; the receipt record (reference, verified flag) stays.</summary>
    public async Task<IActionResult> OnPostDeleteReceiptAsync(Guid id, Guid receiptId)
    {
        var order = await _unitOfWork.Orders.GetWithDetailsAsync(id);
        var receipt = order?.PaymentReceipts.FirstOrDefault(r => r.Id == receiptId);
        if (receipt is null)
        {
            return NotFound();
        }

        if (receipt.HasImage)
        {
            try
            {
                await _fileStorage.DeleteAsync(receipt.ImagePath);
            }
            catch (FileStorageException)
            {
                StatusMessage = "The receipt image could not be deleted right now. Please try again in a moment.";
                return RedirectToPage(new { id });
            }

            receipt.ImagePath = string.Empty;
            await _unitOfWork.SaveChangesAsync();
        }

        StatusMessage = "Receipt image deleted.";
        return RedirectToPage(new { id });
    }

    public async Task<IActionResult> OnPostCancelAsync(Guid id)
    {
        var order = await _unitOfWork.Orders.GetWithDetailsAsync(id);
        if (order is null)
        {
            return NotFound();
        }

        if (!OrderWorkflow.CanCancel(order))
        {
            StatusMessage = "This order can't be cancelled from its current status.";
            return RedirectToPage(new { id });
        }

        // The cancelled order's units go back on the shelf; both changes are saved together.
        await using var transaction = await _unitOfWork.BeginTransactionAsync();

        foreach (var item in order.OrderItems.Where(i => !string.IsNullOrEmpty(i.Size)))
        {
            await _unitOfWork.Products.RestoreStockAsync(item.ProductId, item.ProductColorId, item.Size!, item.Quantity);
        }

        order.OrderStatus = OrderStatus.Cancelled;
        await _unitOfWork.SaveChangesAsync();
        await transaction.CommitAsync();

        StatusMessage = "Order cancelled and its items were put back in stock.";
        return RedirectToPage(new { id });
    }
}
