using ECommerceStore.Core.Entities;
using ECommerceStore.Core.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ECommerceStore.Web.Areas.Admin.Pages.Reviews;

/// <summary>Lists every review so the owner can hide, show or delete it.</summary>
public class IndexModel : PageModel
{
    private readonly IUnitOfWork _unitOfWork;

    public IndexModel(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public IReadOnlyList<Review> Reviews { get; private set; } = Array.Empty<Review>();

    [TempData]
    public string? StatusMessage { get; set; }

    public async Task OnGetAsync()
    {
        Reviews = await _unitOfWork.Reviews.GetAllWithProductAsync();
    }

    public async Task<IActionResult> OnPostToggleAsync(Guid id)
    {
        var review = await _unitOfWork.Reviews.GetByIdAsync(id);
        if (review is not null)
        {
            review.IsHidden = !review.IsHidden;
            await _unitOfWork.SaveChangesAsync();
            StatusMessage = review.IsHidden ? "Review hidden from the site." : "Review is visible on the site again.";
        }

        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostDeleteAsync(Guid id)
    {
        var review = await _unitOfWork.Reviews.GetByIdAsync(id);
        if (review is not null)
        {
            _unitOfWork.Reviews.Remove(review);
            await _unitOfWork.SaveChangesAsync();
            StatusMessage = "Review deleted.";
        }

        return RedirectToPage();
    }
}
