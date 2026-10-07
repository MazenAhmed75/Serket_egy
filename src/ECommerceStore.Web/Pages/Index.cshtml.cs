using ECommerceStore.Core.DTOs;
using ECommerceStore.Core.Interfaces;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ECommerceStore.Web.Pages;

public class IndexModel : PageModel
{
    private readonly IUnitOfWork _unitOfWork;

    public IndexModel(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    /// <summary>One card per active product, in the order set in Admin.</summary>
    public IReadOnlyList<ProductCardDto> Products { get; private set; } = Array.Empty<ProductCardDto>();

    public async Task OnGetAsync()
    {
        var products = await _unitOfWork.Products.GetActiveProductsAsync();
        var ratings = await _unitOfWork.Reviews.GetSummariesAsync();

        Products = products
            .Select(p => new ProductCardDto
            {
                Slug = p.Slug,
                Name = p.Name,
                ShortDescription = p.ShortDescription,
                Price = p.CurrentPrice,
                OriginalPrice = p.Price,
                DiscountPercent = p.HasDiscount ? p.DiscountPercent : 0,
                Rating = ratings.TryGetValue(p.Id, out var rating) ? rating : RatingSummary.None,
                ImageUrl = p.ImageUrl,
                InStock = p.IsInStock,
                Swatches = p.Colors.Where(c => c.IsActive).OrderBy(c => c.Name).Select(c => c.HexCode).ToList()
            })
            .ToList();
    }
}
