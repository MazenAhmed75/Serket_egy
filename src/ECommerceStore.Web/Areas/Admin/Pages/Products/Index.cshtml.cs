using System.ComponentModel.DataAnnotations;
using ECommerceStore.Core.Entities;
using ECommerceStore.Core.Interfaces;
using ECommerceStore.Core.Services;
using ECommerceStore.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ECommerceStore.Web.Areas.Admin.Pages.Products;

/// <summary>All products: the cards shown on the home page. Add, hide/show, delete and open one to edit it.</summary>
public class IndexModel : PageModel
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IFileStorageService _fileStorageService;

    public IndexModel(IUnitOfWork unitOfWork, IFileStorageService fileStorageService)
    {
        _unitOfWork = unitOfWork;
        _fileStorageService = fileStorageService;
    }

    [BindProperty]
    public NewProductInputModel Input { get; set; } = new();

    public IReadOnlyList<ProductRow> Rows { get; private set; } = Array.Empty<ProductRow>();

    /// <summary>True while at least one of the three starter fabrics has not been added yet.</summary>
    public bool CanAddStarterFabrics { get; private set; }

    [TempData]
    public string? StatusMessage { get; set; }

    public async Task OnGetAsync()
    {
        await LoadAsync();
    }

    public async Task<IActionResult> OnPostCreateAsync()
    {
        var hasImage = Input.ImageFile is { Length: > 0 };
        if (hasImage && ImageUploadRules.Validate(Input.ImageFile!) is { } imageError)
        {
            ModelState.AddModelError("Input.ImageFile", imageError);
        }

        if (!ModelState.IsValid)
        {
            await LoadAsync();
            return Page();
        }

        var imageUrl = StarterCatalog.PlaceholderImage;
        if (hasImage)
        {
            await using var stream = Input.ImageFile!.OpenReadStream();
            imageUrl = await _fileStorageService.SaveAsync(stream, Input.ImageFile.FileName, "products");
        }

        var slug = await Slugger.MakeUniqueAsync(Input.Name, s => _unitOfWork.Products.SlugExistsAsync(s));
        var all = await _unitOfWork.Products.GetAllWithDetailsAsync();

        var product = new Product
        {
            Id = Guid.NewGuid(),
            Name = Input.Name.Trim(),
            Slug = slug,
            ShortDescription = string.IsNullOrWhiteSpace(Input.ShortDescription) ? null : Input.ShortDescription.Trim(),
            Description = Input.Description,
            Price = Input.Price,
            ImageUrl = imageUrl,
            SortOrder = all.Count == 0 ? 1 : all.Max(p => p.SortOrder) + 1,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        await _unitOfWork.Products.AddAsync(product);
        await _unitOfWork.SaveChangesAsync();

        StatusMessage = $"\"{product.Name}\" created. Now add its colours and set its stock below (until you set stock it shows as sold out).";
        return RedirectToPage("./Edit", new { id = product.Id });
    }

    public async Task<IActionResult> OnPostAddStarterFabricsAsync()
    {
        var all = await _unitOfWork.Products.GetAllWithDetailsAsync();
        var missing = StarterCatalog.BuildMissing(all.Select(p => p.Slug).ToList());

        if (missing.Count == 0)
        {
            StatusMessage = "The starter fabrics are already there.";
            return RedirectToPage();
        }

        // Starter products take the first free display positions after the existing ones.
        var next = all.Count == 0 ? 1 : all.Max(p => p.SortOrder) + 1;
        foreach (var product in missing)
        {
            product.SortOrder = next++;
            await _unitOfWork.Products.AddAsync(product);
        }

        await _unitOfWork.SaveChangesAsync();

        StatusMessage = $"Added {missing.Count} starter fabric(s). They use a placeholder photo and 10 units per size, so upload real photos and set the real stock.";
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostToggleAsync(Guid id)
    {
        var product = await _unitOfWork.Products.GetByIdAsync(id);
        if (product is not null)
        {
            product.IsActive = !product.IsActive;
            await _unitOfWork.SaveChangesAsync();
            StatusMessage = product.IsActive
                ? $"\"{product.Name}\" is visible on the site again."
                : $"\"{product.Name}\" is hidden from the site.";
        }

        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostDeleteAsync(Guid id)
    {
        var product = await _unitOfWork.Products.GetByIdAsync(id);
        if (product is null)
        {
            return RedirectToPage();
        }

        if (await _unitOfWork.Products.HasOrdersAsync(id))
        {
            // Deleting would break the order history, so the product can only be hidden.
            StatusMessage = $"\"{product.Name}\" has orders, so it can't be deleted. Use Hide instead.";
            return RedirectToPage();
        }

        _unitOfWork.Products.Remove(product);
        await _unitOfWork.SaveChangesAsync();

        StatusMessage = $"\"{product.Name}\" deleted.";
        return RedirectToPage();
    }

    private async Task LoadAsync()
    {
        var products = await _unitOfWork.Products.GetAllWithDetailsAsync();

        Rows = products
            .Select(p => new ProductRow(
                p.Id,
                p.Name,
                p.Slug,
                p.ShortDescription,
                p.Price,
                p.ImageUrl,
                p.SortOrder,
                p.IsActive,
                p.UnitsInStock,
                p.Colors.Count(c => c.IsActive)))
            .ToList();

        CanAddStarterFabrics = StarterCatalog.BuildMissing(products.Select(p => p.Slug).ToList()).Count > 0;
    }

    public sealed record ProductRow(
        Guid Id,
        string Name,
        string Slug,
        string? ShortDescription,
        decimal Price,
        string ImageUrl,
        int SortOrder,
        bool IsActive,
        int TotalStock,
        int ColorCount);

    public class NewProductInputModel
    {
        [Required(ErrorMessage = "Name is required.")]
        [StringLength(200)]
        [Display(Name = "Name")]
        public string Name { get; set; } = string.Empty;

        [StringLength(200)]
        [Display(Name = "Short line (shown on the home page card)")]
        public string? ShortDescription { get; set; }

        [Required(ErrorMessage = "Description is required.")]
        [StringLength(4000)]
        public string Description { get; set; } = string.Empty;

        [Range(0.01, 1_000_000, ErrorMessage = "Enter a price greater than 0.")]
        public decimal Price { get; set; }

        [Display(Name = "Photo (optional - you can add it later)")]
        public IFormFile? ImageFile { get; set; }
    }
}
