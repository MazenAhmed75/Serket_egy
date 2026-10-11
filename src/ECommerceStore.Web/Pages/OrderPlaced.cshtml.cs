using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ECommerceStore.Web.Pages;

/// <summary>
/// A plain "your order was placed" page used only if the normal confirmation page can't be reached. It shows the order
/// number and nothing else, so it exposes no personal details.
/// </summary>
public class OrderPlacedModel : PageModel
{
    public string OrderNumber { get; private set; } = string.Empty;

    public void OnGet(string? n)
    {
        // Only characters an order number can contain are shown.
        OrderNumber = new string((n ?? string.Empty).Where(c => char.IsAsciiLetterOrDigit(c) || c == '-').Take(40).ToArray());
    }
}
