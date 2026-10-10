using ECommerceStore.Core.Services;
using ECommerceStore.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.RateLimiting;

namespace ECommerceStore.Web.Pages;

/// <summary>The shopping cart: change quantities, remove lines, then go to checkout.</summary>
[EnableRateLimiting("shopping")]
public class CartModel : PageModel
{
    private readonly CartStore _cartStore;
    private readonly CartService _cartService;

    public CartModel(CartStore cartStore, CartService cartService)
    {
        _cartStore = cartStore;
        _cartService = cartService;
    }

    public CartSummary Summary { get; private set; } = new(Array.Empty<CartItemView>());

    /// <summary>A message from another page (for example "the last units were just bought").</summary>
    [TempData]
    public string? CartNotice { get; set; }

    public async Task OnGetAsync()
    {
        Summary = await _cartService.BuildAsync(_cartStore.Read(HttpContext));
    }

    public IActionResult OnPostUpdate(string key, int quantity)
    {
        _cartStore.Write(HttpContext, _cartStore.Read(HttpContext).SetQuantity(key, quantity));
        return RedirectToPage();
    }

    public IActionResult OnPostRemove(string key)
    {
        _cartStore.Write(HttpContext, _cartStore.Read(HttpContext).Remove(key));
        return RedirectToPage();
    }
}
