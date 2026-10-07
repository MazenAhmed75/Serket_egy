using System.ComponentModel.DataAnnotations;
using ECommerceStore.Core.Constants;
using ECommerceStore.Core.Entities;
using ECommerceStore.Core.Interfaces;
using ECommerceStore.Core.Services;
using ECommerceStore.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ECommerceStore.Web.Areas.Admin.Pages.Products;

/// <summary>
/// Edits one product: its home-page card (name, short line, photo, price, order), its details
/// (description), extra photos, colours (with photos) and stock per colour and size.
/// </summary>
public class EditModel : PageModel
{
    private const int MaxGalleryImages = 8;

    private readonly IUnitOfWork _unitOfWork;
    private readonly IFileStorageService _fileStorageService;
    private readonly StockReminderService _stockReminderService;

    public EditModel(IUnitOfWork unitOfWork, IFileStorageService fileStorageService, StockReminderService stockReminderService)
    {
        _unitOfWork = unitOfWork;
        _fileStorageService = fileStorageService;
        _stockReminderService = stockReminderService;
    }

    /// <summary>The product being edited (from the page address).</summary>
    [BindProperty(SupportsGet = true)]
    public Guid Id { get; set; }

    [BindProperty]
    public ProductInputModel Input { get; set; } = new();

    public string ProductSlug { get; private set; } = string.Empty;

    [BindProperty]
    public ColorInputModel NewColor { get; set; } = new();

    public Guid? ExistingProductId { get; private set; }

    public string? CurrentImageUrl { get; private set; }

    public IReadOnlyList<ProductColor> Colors { get; private set; } = Array.Empty<ProductColor>();

    public IReadOnlyList<ProductImage> GalleryImages { get; private set; } = Array.Empty<ProductImage>();

    [BindProperty]
    public List<IFormFile> GalleryFiles { get; set; } = new();

    /// <summary>Photo for a new colour (optional).</summary>
    [BindProperty]
    public IFormFile? NewColorImage { get; set; }

    /// <summary>One row per colour (or a single "all" row when the product has no colours) with units per size.</summary>
    public IReadOnlyList<StockRow> StockRows { get; private set; } = Array.Empty<StockRow>();

    public IReadOnlyList<string> Sizes => ProductSizes.All;

    [TempData]
    public string? StatusMessage { get; set; }

    public async Task<IActionResult> OnGetAsync()
    {
        var product = await _unitOfWork.Products.GetWithColorsAsync(Id);
        if (product is null)
        {
            return NotFound();
        }

        Fill(product);
        Input = new ProductInputModel
        {
            Name = product.Name,
            ShortDescription = product.ShortDescription,
            Description = product.Description,
            Price = product.Price,
            SortOrder = product.SortOrder,
            DiscountActive = product.DiscountActive,
            DiscountPercent = product.DiscountPercent == 0 ? 10 : product.DiscountPercent,
            IsActive = product.IsActive
        };

        return Page();
    }

    /// <summary>Loads everything the page shows next to the form.</summary>
    private void Fill(Product product)
    {
        ExistingProductId = product.Id;
        ProductSlug = product.Slug;
        CurrentImageUrl = product.ImageUrl;
        Colors = product.Colors.OrderBy(c => c.Name).ToList();
        GalleryImages = product.Images.OrderBy(i => i.SortOrder).ToList();
        StockRows = BuildStockRows(product);
    }

    private IActionResult Back() => RedirectToPage(new { id = Id });

    public async Task<IActionResult> OnPostAsync()
    {
        var existing = await _unitOfWork.Products.GetWithColorsAsync(Id);
        if (existing is null)
        {
            return NotFound();
        }

        var hasNewImage = Input.ImageFile is not null && Input.ImageFile.Length > 0;
        if (hasNewImage && ImageUploadRules.Validate(Input.ImageFile!) is { } imageError)
        {
            ModelState.AddModelError("Input.ImageFile", imageError);
        }

        if (!ModelState.IsValid)
        {
            Fill(existing);
            return Page();
        }

        if (hasNewImage)
        {
            existing.ImageUrl = await SaveImageAsync(Input.ImageFile!);
        }

        existing.Name = Input.Name.Trim();
        existing.ShortDescription = string.IsNullOrWhiteSpace(Input.ShortDescription) ? null : Input.ShortDescription.Trim();
        existing.Description = Input.Description;
        existing.Price = Input.Price;
        existing.SortOrder = Input.SortOrder;
        existing.DiscountActive = Input.DiscountActive;
        existing.DiscountPercent = Input.DiscountPercent;
        existing.IsActive = Input.IsActive;
        _unitOfWork.Products.Update(existing);

        await _unitOfWork.SaveChangesAsync();

        StatusMessage = "Product saved.";
        return Back();
    }

    public async Task<IActionResult> OnPostAddColorAsync()
    {
        var product = await _unitOfWork.Products.GetWithColorsAsync(Id);
        if (product is null)
        {
            StatusMessage = "Save the product before adding colours.";
            return Back();
        }

        if (string.IsNullOrWhiteSpace(NewColor.Name))
        {
            StatusMessage = "Enter a colour name.";
            return Back();
        }

        string? imageUrl = null;
        if (NewColorImage is { Length: > 0 })
        {
            var imageError = ImageUploadRules.Validate(NewColorImage);
            if (imageError is not null)
            {
                StatusMessage = imageError;
                return Back();
            }

            imageUrl = await SaveImageAsync(NewColorImage);
        }

        await _unitOfWork.ProductColors.AddAsync(new ProductColor
        {
            Id = Guid.NewGuid(),
            ProductId = product.Id,
            Name = NewColor.Name.Trim(),
            HexCode = string.IsNullOrWhiteSpace(NewColor.HexCode) ? "#CCCCCC" : NewColor.HexCode,
            ImageUrl = imageUrl,
            IsActive = true
        });

        await _unitOfWork.SaveChangesAsync();

        StatusMessage = $"Added \"{NewColor.Name}\". Set its stock per size in the stock table below.";
        return Back();
    }

    public async Task<IActionResult> OnPostSetColorImageAsync(Guid colorId, IFormFile? colorImage)
    {
        var color = await _unitOfWork.ProductColors.GetByIdAsync(colorId);
        if (color is null)
        {
            return Back();
        }

        if (colorImage is null || colorImage.Length == 0)
        {
            StatusMessage = "Choose a photo to upload.";
            return Back();
        }

        var imageError = ImageUploadRules.Validate(colorImage);
        if (imageError is not null)
        {
            StatusMessage = imageError;
            return Back();
        }

        color.ImageUrl = await SaveImageAsync(colorImage);
        await _unitOfWork.SaveChangesAsync();

        StatusMessage = $"Photo saved for \"{color.Name}\".";
        return Back();
    }

    public async Task<IActionResult> OnPostRemoveColorImageAsync(Guid colorId)
    {
        var color = await _unitOfWork.ProductColors.GetByIdAsync(colorId);
        if (color is not null)
        {
            color.ImageUrl = null;
            await _unitOfWork.SaveChangesAsync();
            StatusMessage = $"Photo removed from \"{color.Name}\".";
        }

        return Back();
    }

    /// <param name="stockInputs">Posted stock numbers, keyed "{colourId or none}_{size}", e.g. "0b1c..._M" or "none_2XL".
    /// They are bound here, with an explicit name, and not as a page-wide property: a page-wide dictionary also tried
    /// to read every other form field (including the security tokens) as a number and made "Save product" fail.</param>
    public async Task<IActionResult> OnPostSaveStockAsync([FromForm(Name = "StockInputs")] Dictionary<string, int> stockInputs)
    {
        var product = await _unitOfWork.Products.GetWithColorsAsync(Id);
        if (product is null)
        {
            StatusMessage = "Save the product before setting stock.";
            return Back();
        }

        var restocked = new List<(Guid? ColorId, string Size)>();

        // One group per colour, or a single group (null colour) when the product has none.
        var groups = product.Colors.Count > 0
            ? product.Colors.Select(c => (ColorId: (Guid?)c.Id, Key: c.Id.ToString())).ToList()
            : new List<(Guid? ColorId, string Key)> { (null, "none") };

        foreach (var (colorId, key) in groups)
        {
            foreach (var size in ProductSizes.All)
            {
                var row = product.Stock.FirstOrDefault(r => r.ProductColorId == colorId && r.Size == size);

                // A box left out of the post (or not a number) keeps whatever stock is already saved.
                if (!stockInputs.TryGetValue($"{key}_{size}", out var quantity))
                {
                    continue;
                }

                quantity = Math.Clamp(quantity, 0, 100_000);

                if (quantity > 0 && (row?.Quantity ?? 0) <= 0)
                {
                    restocked.Add((colorId, size));
                }

                if (row is null)
                {
                    await _unitOfWork.ProductStocks.AddAsync(new ProductStock
                    {
                        Id = Guid.NewGuid(),
                        ProductId = product.Id,
                        ProductColorId = colorId,
                        Size = size,
                        Quantity = quantity
                    });
                }
                else
                {
                    row.Quantity = quantity;
                }
            }
        }

        await _unitOfWork.SaveChangesAsync();

        var productUrl = Url.Page("/Product", null, new { slug = product.Slug, area = "" }, Request.Scheme) ?? string.Empty;
        var emailed = await _stockReminderService.NotifyRestockedAsync(product, restocked, productUrl);

        StatusMessage = emailed > 0 ? $"Stock saved. {emailed} customer(s) were e-mailed that it is back." : "Stock saved.";
        return Back();
    }

    public async Task<IActionResult> OnPostDeleteColorAsync(Guid colorId)
    {
        var color = await _unitOfWork.ProductColors.GetByIdAsync(colorId);
        if (color is not null)
        {
            _unitOfWork.ProductColors.Remove(color);
            await _unitOfWork.SaveChangesAsync();
            StatusMessage = $"Removed \"{color.Name}\".";
        }

        return Back();
    }

    public async Task<IActionResult> OnPostAddGalleryImagesAsync()
    {
        var product = await _unitOfWork.Products.GetWithColorsAsync(Id);
        if (product is null)
        {
            StatusMessage = "Save the product before adding photos.";
            return Back();
        }

        var files = GalleryFiles.Where(f => f.Length > 0).ToList();
        if (files.Count == 0)
        {
            StatusMessage = "Choose at least one photo to upload.";
            return Back();
        }

        if (product.Images.Count + files.Count > MaxGalleryImages)
        {
            StatusMessage = $"A product can have up to {MaxGalleryImages} extra photos.";
            return Back();
        }

        var problem = files.Select(ImageUploadRules.Validate).FirstOrDefault(message => message is not null);
        if (problem is not null)
        {
            StatusMessage = $"{problem} Nothing was uploaded.";
            return Back();
        }

        var nextOrder = product.Images.Count == 0 ? 1 : product.Images.Max(i => i.SortOrder) + 1;
        foreach (var file in files)
        {
            await using var stream = file.OpenReadStream();
            var path = await _fileStorageService.SaveAsync(stream, file.FileName, "products");
            await _unitOfWork.ProductImages.AddAsync(new ProductImage
            {
                Id = Guid.NewGuid(),
                ProductId = product.Id,
                ImageUrl = path,
                SortOrder = nextOrder++
            });
        }

        await _unitOfWork.SaveChangesAsync();
        StatusMessage = files.Count == 1 ? "Photo added." : $"{files.Count} photos added.";
        return Back();
    }

    public async Task<IActionResult> OnPostDeleteGalleryImageAsync(Guid imageId)
    {
        var image = await _unitOfWork.ProductImages.GetByIdAsync(imageId);
        if (image is not null)
        {
            _unitOfWork.ProductImages.Remove(image);
            await _unitOfWork.SaveChangesAsync();
            StatusMessage = "Photo removed.";
        }

        return Back();
    }

    private static List<StockRow> BuildStockRows(Product product)
    {
        StockRow RowFor(string label, string key, Guid? colorId, string? hex)
        {
            return new StockRow(label, key, hex, product.UnitsBySize(colorId));
        }

        return product.Colors.Count > 0
            ? product.Colors.OrderBy(c => c.Name).Select(c => RowFor(c.Name, c.Id.ToString(), c.Id, c.HexCode)).ToList()
            : new List<StockRow> { RowFor("All units", "none", null, null) };
    }

    private async Task<string> SaveImageAsync(IFormFile file)
    {
        await using var stream = file.OpenReadStream();
        return await _fileStorageService.SaveAsync(stream, file.FileName, "products");
    }

    public sealed record StockRow(string Label, string Key, string? HexCode, IReadOnlyDictionary<string, int> Units);

    public class ProductInputModel
    {
        [Required(ErrorMessage = "Name is required.")]
        [StringLength(200)]
        public string Name { get; set; } = string.Empty;

        [StringLength(200)]
        [Display(Name = "Short line (shown on the home page card)")]
        public string? ShortDescription { get; set; }

        [Required(ErrorMessage = "Description is required.")]
        [StringLength(4000)]
        public string Description { get; set; } = string.Empty;

        [Range(0.01, 1_000_000, ErrorMessage = "Enter a price greater than 0.")]
        public decimal Price { get; set; }

        [Range(0, 1000)]
        [Display(Name = "Order on the home page (1 comes first)")]
        public int SortOrder { get; set; }

        [Range(1, 99, ErrorMessage = "The discount must be between 1 and 99 percent.")]
        [Display(Name = "Discount percentage")]
        public int DiscountPercent { get; set; } = 10;

        [Display(Name = "Show the discount banner and charge the lower price")]
        public bool DiscountActive { get; set; }

        [Display(Name = "Active (visible on the storefront)")]
        public bool IsActive { get; set; } = true;

        [Display(Name = "Product photo")]
        public IFormFile? ImageFile { get; set; }
    }

    public class ColorInputModel
    {
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;

        [StringLength(20)]
        [Display(Name = "Swatch colour")]
        public string HexCode { get; set; } = "#CCCCCC";
    }
}
