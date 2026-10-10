using System.ComponentModel.DataAnnotations;
using ECommerceStore.Core.Interfaces;
using ECommerceStore.Core.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ECommerceStore.Web.Areas.Admin.Pages;

/// <summary>Store-wide settings the owner can change without touching code (currently the shipping fee).</summary>
public class SettingsModel : PageModel
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ReceiptCleaner _receiptCleaner;

    public SettingsModel(IUnitOfWork unitOfWork, ReceiptCleaner receiptCleaner)
    {
        _unitOfWork = unitOfWork;
        _receiptCleaner = receiptCleaner;
    }

    [BindProperty]
    public SettingsInputModel Input { get; set; } = new();

    [TempData]
    public string? StatusMessage { get; set; }

    public async Task OnGetAsync()
    {
        var settings = await _unitOfWork.Settings.GetCurrentAsync();
        Input.ShippingFee = settings.ShippingFee;
        Input.ReceiptRetentionDays = settings.ReceiptRetentionDays;
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        var settings = await _unitOfWork.Settings.GetCurrentAsync();
        settings.ShippingFee = Math.Round(Input.ShippingFee, 2);
        settings.ReceiptRetentionDays = Input.ReceiptRetentionDays;
        await _unitOfWork.SaveChangesAsync();

        StatusMessage = "Settings saved.";
        return RedirectToPage();
    }

    /// <summary>Deletes the receipt images of every shipped or cancelled order now (to free storage space).</summary>
    public async Task<IActionResult> OnPostCleanReceiptsNowAsync()
    {
        var result = await _receiptCleaner.DeleteImagesAsync(DateTime.UtcNow);

        StatusMessage = result.Failed > 0
            ? $"{result.Deleted} receipt image(s) deleted; {result.Failed} could not be deleted right now. Try again later."
            : $"{result.Deleted} receipt image(s) deleted.";
        return RedirectToPage();
    }

    public class SettingsInputModel
    {
        [Range(0, 100_000, ErrorMessage = "Enter a shipping fee between 0 and 100,000.")]
        [Display(Name = "Shipping fee (EGP)")]
        public decimal ShippingFee { get; set; }

        [Range(0, 3650, ErrorMessage = "Enter a number of days between 0 and 3650 (0 keeps receipt images forever).")]
        [Display(Name = "Delete receipt images after (days)")]
        public int ReceiptRetentionDays { get; set; }
    }
}
