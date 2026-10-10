using System.Text.Json;
using ECommerceStore.Core.Constants;

namespace ECommerceStore.Core.Services;

/// <summary>Turns a cart into text (kept in a protected cookie) and back. Anything unreadable or invalid becomes an empty cart.</summary>
public static class CartSerializer
{
    private sealed record Dto(Guid P, Guid? C, string S, string G, int Q);

    public static string Serialize(Cart cart) =>
        JsonSerializer.Serialize(cart.Lines.Select(l => new Dto(l.ProductId, l.ColorId, l.Size, l.Gender, l.Quantity)));

    public static Cart Deserialize(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return Cart.Empty;
        }

        Dto[]? items;
        try
        {
            items = JsonSerializer.Deserialize<Dto[]>(text);
        }
        catch (JsonException)
        {
            return Cart.Empty;
        }

        var cart = Cart.Empty;
        foreach (var item in items ?? Array.Empty<Dto>())
        {
            if (item is null || item.P == Guid.Empty || !ProductSizes.IsValid(item.S) || !ProductGenders.IsValid(item.G) || item.Q < 1)
            {
                continue;
            }

            // Duplicates merge; lines beyond the cart limit are dropped.
            cart.TryAdd(new CartLine(item.P, item.C, item.S, item.G, item.Q), out cart);
        }

        return cart;
    }
}
