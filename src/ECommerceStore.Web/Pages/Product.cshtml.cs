using System.Text.Json;
using ECommerceStore.Core.Constants;
using ECommerceStore.Core.DTOs;
using ECommerceStore.Core.Entities;
using ECommerceStore.Core.Interfaces;
using ECommerceStore.Core.Services;
using ECommerceStore.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.RateLimiting;

namespace ECommerceStore.Web.Pages;

[EnableRateLimiting("shopping")]
public class ProductModel : PageModel
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ReviewService _reviewService;
    private readonly StockReminderService _stockReminderService;

    public ProductModel(IUnitOfWork unitOfWork, ReviewService reviewService, StockReminderService stockReminderService)
    {
        _unitOfWork = unitOfWork;
        _reviewService = reviewService;
        _stockReminderService = stockReminderService;
    }

    public ProductDisplayDto Product { get; private set; } = null!;

    // Selections restored when the customer taps "Change" on the checkout page.
    public string SelectedGender { get; private set; } = ProductGenders.Default;
    public string SelectedSize { get; private set; } = ProductSizes.Default;
    public Guid? SelectedColorId { get; private set; }
    public int SelectedQuantity { get; private set; } = 1;

    /// <summary>Highest quantity the stepper allows for the pre-selected options (the script keeps it up to date afterwards).</summary>
    public int InitialMaxQuantity { get; private set; } = 1;

    /// <summary>Stock per colour and size as JSON, read by product-options.js to grey out sold-out sizes.</summary>
    public string StockJson { get; private set; } = "{}";

    /// <summary>True when a shopper is logged in (reviews need an account).</summary>
    public bool IsLoggedIn => HttpContext.GetCustomerId() is not null;

    /// <summary>True when the logged-in shopper has already reviewed this product.</summary>
    public bool HasReviewed { get; private set; }

    /// <summary>The logged-in shopper's e-mail, to pre-fill the "remind me" box.</summary>
    public string? CustomerEmail { get; private set; }

    /// <summary>Where to send the shopper back to after logging in or signing up.</summary>
    public string PageUrl => $"/product/{Product.Slug}";

    [TempData]
    public string? ReviewMessage { get; set; }

    [TempData]
    public bool ReviewSucceeded { get; set; }

    /// <summary>The size guides (cm) the page shows; the matching one is displayed for the chosen fit.</summary>
    public IReadOnlyList<SizeChart> SizeCharts => Core.Constants.SizeCharts.All;

    public async Task<IActionResult> OnGetAsync(string slug, string? gender = null, string? size = null, Guid? colorId = null, int quantity = 1)
    {
        if (ProductGenders.IsValid(gender)) SelectedGender = gender!;
        if (ProductSizes.IsValid(size)) SelectedSize = size!;
        SelectedQuantity = Math.Max(quantity, 1);

        var product = await _unitOfWork.Products.GetActiveBySlugAsync(slug);
        if (product is null)
        {
            return NotFound();
        }

        await LoadAsync(product);

        // Only keep the colour if it is actually one of this product's active colours.
        if (colorId.HasValue && Product.Colors.Any(c => c.Id == colorId.Value))
        {
            SelectedColorId = colorId;
        }
        else if (Product.Colors.Count == 1 && Product.Colors[0].InStock)
        {
            // A product that comes in a single colour doesn't make the customer pick it.
            SelectedColorId = Product.Colors[0].Id;
        }

        var selectedStock = SelectedColorId.HasValue
            ? Product.Colors.First(c => c.Id == SelectedColorId.Value).Stock
            : (Product.HasColors ? null : Product.DefaultStock);

        InitialMaxQuantity = selectedStock is not null && selectedStock.TryGetValue(SelectedSize, out var units)
            ? Math.Max(units, 1)
            : 1;
        SelectedQuantity = Math.Min(SelectedQuantity, InitialMaxQuantity);

        StockJson = JsonSerializer.Serialize(new
        {
            hasColors = Product.HasColors,
            none = Product.DefaultStock,
            colors = Product.Colors.ToDictionary(
                c => c.Id.ToString(),
                c => new { name = c.Name, stock = c.Stock, image = c.ImageUrl })
        });

        return Page();
    }

    /// <summary>"Remind me when it's back": saves the e-mail for one sold-out colour and size. Returns JSON for the page script.</summary>
    public async Task<IActionResult> OnPostRemindAsync(string slug, string? variant, string? email)
    {
        var product = await _unitOfWork.Products.GetActiveBySlugAsync(slug);
        if (product is null)
        {
            return NotFound();
        }

        // The variant arrives as "colourId|size" (or "none|size" for a product without colours).
        var parts = (variant ?? string.Empty).Split('|');
        Guid? colorId = parts.Length == 2 && Guid.TryParse(parts[0], out var parsedColor) ? parsedColor : null;
        var size = parts.Length == 2 ? parts[1] : null;

        var result = await _stockReminderService.SubscribeAsync(product, colorId, size, email);
        return new JsonResult(new { ok = result.Success, message = result.Message });
    }

    /// <summary>Saves the logged-in shopper's review, then returns to the reviews section.</summary>
    public async Task<IActionResult> OnPostReviewAsync(string slug, int rating, string? comment, bool anonymous)
    {
        var product = await _unitOfWork.Products.GetActiveBySlugAsync(slug);
        if (product is null)
        {
            return NotFound();
        }

        var customerId = HttpContext.GetCustomerId();
        if (customerId is null)
        {
            return Redirect($"/Account/Login?returnUrl={Uri.EscapeDataString($"/product/{product.Slug}")}");
        }

        var result = await _reviewService.SubmitAsync(product.Id, customerId.Value, rating, comment, anonymous);
        ReviewSucceeded = result.Success;
        ReviewMessage = result.Success ? "Thank you! Your review has been posted." : result.Error;

        return Redirect($"/product/{product.Slug}#reviews");
    }

    /// <summary>Fills <see cref="Product"/> (and the review/e-mail details around it) from the stored product.</summary>
    private async Task LoadAsync(Product product)
    {
        var reviews = await _unitOfWork.Reviews.GetVisibleForProductAsync(product.Id);
        var summaries = await _unitOfWork.Reviews.GetSummariesAsync();

        Product = new ProductDisplayDto
        {
            Id = product.Id,
            Slug = product.Slug,
            Name = product.Name,
            Description = product.Description,
            Price = product.CurrentPrice,
            OriginalPrice = product.Price,
            DiscountPercent = product.HasDiscount ? product.DiscountPercent : 0,
            Rating = summaries.TryGetValue(product.Id, out var summary) ? summary : RatingSummary.None,
            Reviews = reviews.Select(r => new ReviewDto(r.PublicName, r.Rating, r.Comment, r.CreatedAt)).ToList(),
            ImageUrl = product.ImageUrl,
            GalleryImages = new[] { product.ImageUrl }
                .Concat(product.Images.OrderBy(i => i.SortOrder).Select(i => i.ImageUrl))
                .Where(u => !string.IsNullOrWhiteSpace(u))
                .ToList(),
            DefaultStock = product.UnitsBySize(null),
            Colors = product.Colors
                .Where(c => c.IsActive)
                .OrderBy(c => c.Name)
                .Select(c => new ProductColorDto
                {
                    Id = c.Id,
                    Name = c.Name,
                    HexCode = c.HexCode,
                    ImageUrl = c.ImageUrl,
                    Stock = product.UnitsBySize(c.Id)
                })
                .ToList()
        };

        var customerId = HttpContext.GetCustomerId();
        if (customerId is not null)
        {
            HasReviewed = await _unitOfWork.Reviews.HasReviewedAsync(product.Id, customerId.Value);
            CustomerEmail = (await _unitOfWork.Customers.GetByIdAsync(customerId.Value))?.Email;
        }
    }
}
