using ECommerceStore.Core.Interfaces;

namespace ECommerceStore.Core.Services;

/// <summary>A cart line with everything the cart and checkout pages show, read fresh from the database.</summary>
public sealed record CartItemView(
    CartLine Line,
    string Slug,
    string ProductName,
    string? ColorName,
    string? ColorHex,
    string ImageUrl,
    decimal UnitPrice,
    int Available,
    string? Problem)
{
    public decimal LineTotal => UnitPrice * Line.Quantity;
}

public sealed record CartSummary(IReadOnlyList<CartItemView> Items)
{
    public decimal Subtotal => Items.Sum(i => i.LineTotal);

    public int ItemCount => Items.Sum(i => i.Line.Quantity);

    public bool IsEmpty => Items.Count == 0;

    /// <summary>True when any line can't be bought as it is (sold out, not enough left, product or colour removed).</summary>
    public bool HasProblems => Items.Any(i => i.Problem is not null);
}

/// <summary>Works out what a cart really costs and whether every line can still be bought.</summary>
public class CartService
{
    private readonly IUnitOfWork _unitOfWork;

    public CartService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<CartSummary> BuildAsync(Cart cart)
    {
        if (cart.IsEmpty)
        {
            return new CartSummary(Array.Empty<CartItemView>());
        }

        var ids = cart.Lines.Select(l => l.ProductId).Distinct().ToList();
        var products = (await _unitOfWork.Products.GetActiveByIdsAsync(ids)).ToDictionary(p => p.Id);

        var items = new List<CartItemView>();
        foreach (var line in cart.Lines)
        {
            if (!products.TryGetValue(line.ProductId, out var product))
            {
                items.Add(new CartItemView(line, string.Empty, "This product", null, null, StarterCatalog.PlaceholderImage, 0, 0,
                    "This product is no longer available."));
                continue;
            }

            string? problem = null;
            var activeColors = product.Colors.Where(c => c.IsActive).ToList();
            var color = activeColors.FirstOrDefault(c => c.Id == line.ColorId);

            if (activeColors.Count > 0 && color is null)
            {
                problem = "Please choose the colour again.";
            }
            else if (activeColors.Count == 0 && line.ColorId is not null)
            {
                problem = "This colour is no longer available.";
            }

            var available = problem is null ? product.UnitsAvailable(color?.Id, line.Size) : 0;
            if (problem is null && available <= 0)
            {
                problem = "Sold out.";
            }
            else if (problem is null && line.Quantity > available)
            {
                problem = $"Only {available} left.";
            }

            items.Add(new CartItemView(
                line,
                product.Slug,
                product.Name,
                color?.Name,
                color?.HexCode,
                string.IsNullOrEmpty(color?.ImageUrl) ? product.ImageUrl : color.ImageUrl,
                product.CurrentPrice,
                available,
                problem));
        }

        return new CartSummary(items);
    }
}
