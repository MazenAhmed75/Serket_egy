using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;
using ECommerceStore.Core.Entities;
using ECommerceStore.Core.Interfaces;
using ECommerceStore.Core.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ECommerceStore.Web.Areas.Admin.Pages.Promos;

public class IndexModel : PageModel
{
    private static readonly Regex CodePattern = new("^[A-Z0-9_-]{3,50}$", RegexOptions.Compiled);

    private readonly IUnitOfWork _unitOfWork;

    public IndexModel(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    [BindProperty]
    public PromoInputModel Input { get; set; } = new();

    public IReadOnlyList<PromoRow> Rows { get; private set; } = Array.Empty<PromoRow>();

    [TempData]
    public string? StatusMessage { get; set; }

    public async Task OnGetAsync()
    {
        await LoadRowsAsync();
    }

    public async Task<IActionResult> OnPostCreateAsync()
    {
        var code = PromoCodeService.Normalize(Input.Code);

        if (!CodePattern.IsMatch(code))
        {
            ModelState.AddModelError("Input.Code", "Use 3-50 letters, numbers, dashes or underscores (no spaces).");
        }

        if (!Input.GivesPercentDiscount && !Input.GivesFreeShipping)
        {
            ModelState.AddModelError(string.Empty, "Tick at least one thing the code does: a percentage discount, free shipping, or both.");
        }

        if (Input.GivesPercentDiscount && (Input.PercentOff < 1 || Input.PercentOff > 100))
        {
            ModelState.AddModelError("Input.PercentOff", "The percentage must be between 1 and 100.");
        }

        if (ModelState.IsValid && await _unitOfWork.PromoCodes.GetByCodeAsync(code) is not null)
        {
            ModelState.AddModelError("Input.Code", "A promo code with this name already exists.");
        }

        if (!ModelState.IsValid)
        {
            await LoadRowsAsync();
            return Page();
        }

        await _unitOfWork.PromoCodes.AddAsync(new PromoCode
        {
            Id = Guid.NewGuid(),
            Code = code,
            PercentOff = Input.GivesPercentDiscount ? Input.PercentOff : null,
            FreeShipping = Input.GivesFreeShipping,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        });
        await _unitOfWork.SaveChangesAsync();

        StatusMessage = $"Promo code {code} created.";
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostToggleAsync(Guid id)
    {
        var promo = await _unitOfWork.PromoCodes.GetByIdAsync(id);
        if (promo is not null)
        {
            promo.IsActive = !promo.IsActive;
            await _unitOfWork.SaveChangesAsync();
            StatusMessage = promo.IsActive ? $"{promo.Code} is active again." : $"{promo.Code} is switched off.";
        }

        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostDeleteAsync(Guid id)
    {
        var promo = await _unitOfWork.PromoCodes.GetByIdAsync(id);
        if (promo is null)
        {
            return RedirectToPage();
        }

        var uses = await _unitOfWork.PromoCodes.CountRedemptionsAsync(promo.Id);
        if (uses > 0)
        {
            // Deleting would erase the record that stops those customers from using it again.
            StatusMessage = $"{promo.Code} has been used {uses} time(s), so it can't be deleted. Switch it off instead.";
            return RedirectToPage();
        }

        _unitOfWork.PromoCodes.Remove(promo);
        await _unitOfWork.SaveChangesAsync();

        StatusMessage = $"{promo.Code} deleted.";
        return RedirectToPage();
    }

    private async Task LoadRowsAsync()
    {
        var promos = await _unitOfWork.PromoCodes.GetAllOrderedAsync();
        var rows = new List<PromoRow>();
        foreach (var promo in promos)
        {
            rows.Add(new PromoRow(promo, await _unitOfWork.PromoCodes.CountRedemptionsAsync(promo.Id)));
        }

        Rows = rows;
    }

    public sealed record PromoRow(PromoCode Promo, int Uses);

    public class PromoInputModel
    {
        [Required(ErrorMessage = "Type the promo code.")]
        [StringLength(50)]
        [Display(Name = "Promo code")]
        public string Code { get; set; } = string.Empty;

        [Display(Name = "Takes a percentage off the items")]
        public bool GivesPercentDiscount { get; set; } = true;

        [Display(Name = "Percentage off")]
        public int PercentOff { get; set; } = 10;

        [Display(Name = "Gives free shipping")]
        public bool GivesFreeShipping { get; set; }
    }
}
