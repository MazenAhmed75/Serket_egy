using ECommerceStore.Core.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ECommerceStore.Web.Areas.Admin.Pages.Orders;

/// <summary>
/// Shows a payment receipt image. Receipts are private files kept outside the web root, so the only way to see
/// one is through this page, which sits inside the Admin area and therefore needs the admin login.
/// </summary>
public class ReceiptModel : PageModel
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IFileStorageService _fileStorage;

    public ReceiptModel(IUnitOfWork unitOfWork, IFileStorageService fileStorage)
    {
        _unitOfWork = unitOfWork;
        _fileStorage = fileStorage;
    }

    public async Task<IActionResult> OnGetAsync(Guid id)
    {
        var receipt = await _unitOfWork.PaymentReceipts.GetByIdAsync(id);
        if (receipt is null)
        {
            return NotFound();
        }

        // Receipts saved before they became private are still ordinary public paths.
        if (receipt.ImagePath.StartsWith('/'))
        {
            return LocalRedirect(receipt.ImagePath);
        }

        var stream = _fileStorage.OpenPrivate(receipt.ImagePath);
        if (stream is null)
        {
            return NotFound();
        }

        var contentType = Path.GetExtension(receipt.ImagePath).ToLowerInvariant() switch
        {
            ".png" => "image/png",
            ".webp" => "image/webp",
            _ => "image/jpeg"
        };

        // Never cache a payment receipt in a shared or browser cache.
        Response.Headers.CacheControl = "private, no-store";
        return File(stream, contentType);
    }
}
