using System.ComponentModel.DataAnnotations;
using ECommerceStore.Core.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ECommerceStore.Web.Areas.Admin.Pages;

/// <summary>Store-wide settings the owner can change without touching code (currently the shipping fee).</summary>
public class SettingsModel : PageModel
{
    private readonly IUnitOfWork _unitOfWork;

    public SettingsModel(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    [BindProperty]
    public SettingsInputModel Input { get; set; } = new();

    [TempData]
    public string? StatusMessage { get; set; }

    public async Task OnGetAsync()
    {
        var settings = await _unitOfWork.Settings.GetCurrentAsync();
        Input.ShippingFee = settings.ShippingFee;
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        var settings = await _unitOfWork.Settings.GetCurrentAsync();
        settings.ShippingFee = Math.Round(Input.ShippingFee, 2);
        await _unitOfWork.SaveChangesAsync();

        StatusMessage = "Settings saved.";
        return RedirectToPage();
    }

    public class SettingsInputModel
    {
        [Range(0, 100_000, ErrorMessage = "Enter a shipping fee between 0 and 100,000.")]
        [Display(Name = "Shipping fee (EGP)")]
        public decimal ShippingFee { get; set; }
    }
}
