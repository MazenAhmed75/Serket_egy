using ECommerceStore.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ECommerceStore.Web.Pages.Account;

public class LogoutModel : PageModel
{
    // Logging out is a POST (with the antiforgery token) so another site can't log a shopper out with a link.
    public IActionResult OnGet() => RedirectToPage("/Index");

    public async Task<IActionResult> OnPostAsync()
    {
        await HttpContext.SignOutCustomerAsync();
        return RedirectToPage("/Index");
    }
}
