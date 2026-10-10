namespace ECommerceStore.Core.Services;

/// <summary>One line of the cart: a product in one colour, size and fit, with a quantity. Prices are never stored here.</summary>
public sealed record CartLine(Guid ProductId, Guid? ColorId, string Size, string Gender, int Quantity)
{
    /// <summary>Two lines with the same key are the same item and are merged (quantities add up).</summary>
    public string Key => $"{ProductId:N}-{(ColorId.HasValue ? ColorId.Value.ToString("N") : "none")}-{Size}-{Gender}";
}

/// <summary>
/// The shopping cart. It only remembers WHAT was chosen; prices, names and stock are always read fresh from the
/// database, so a cart can never carry a wrong price. Instances are never changed: every change returns a new cart.
/// </summary>
public sealed class Cart
{
    public const int MaxLines = 12;
    public const int MaxQuantityPerLine = 20;

    public static readonly Cart Empty = new(Array.Empty<CartLine>());

    private readonly IReadOnlyList<CartLine> _lines;

    public Cart(IEnumerable<CartLine> lines)
    {
        _lines = lines.ToList();
    }

    public IReadOnlyList<CartLine> Lines => _lines;

    public bool IsEmpty => _lines.Count == 0;

    /// <summary>Total number of units across all lines (the number shown on the cart icon).</summary>
    public int ItemCount => _lines.Sum(l => l.Quantity);

    public int QuantityOf(string key) => _lines.FirstOrDefault(l => l.Key == key)?.Quantity ?? 0;

    /// <summary>Adds the line, or adds to the quantity of the identical line. False when the cart is full of other items.</summary>
    public bool TryAdd(CartLine line, out Cart result)
    {
        var existing = _lines.FirstOrDefault(l => l.Key == line.Key);
        if (existing is null)
        {
            if (_lines.Count >= MaxLines)
            {
                result = this;
                return false;
            }

            result = new Cart(_lines.Append(line with { Quantity = Math.Clamp(line.Quantity, 1, MaxQuantityPerLine) }));
            return true;
        }

        var quantity = Math.Clamp(existing.Quantity + line.Quantity, 1, MaxQuantityPerLine);
        result = new Cart(_lines.Select(l => l.Key == line.Key ? l with { Quantity = quantity } : l));
        return true;
    }

    /// <summary>Sets a line's quantity; zero or less removes it.</summary>
    public Cart SetQuantity(string key, int quantity) =>
        quantity <= 0
            ? Remove(key)
            : new Cart(_lines.Select(l => l.Key == key ? l with { Quantity = Math.Min(quantity, MaxQuantityPerLine) } : l));

    public Cart Remove(string key) => new(_lines.Where(l => l.Key != key));
}
