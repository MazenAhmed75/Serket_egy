using System.ComponentModel.DataAnnotations;
using ECommerceStore.Core.Interfaces;
using ECommerceStore.Infrastructure.Services;
using ECommerceStore.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.RateLimiting;

namespace ECommerceStore.Web.Pages.Account;

[EnableRateLimiting("auth")]
public class LoginModel : PageModel
{
    private readonly IUnitOfWork _unitOfWork;

    public LoginModel(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    [BindProperty]
    public LoginInputModel Input { get; set; } = new();

    [BindProperty(SupportsGet = true)]
    public string? ReturnUrl { get; set; }

    public IActionResult OnGet()
    {
        if (HttpContext.GetCustomerId() is not null)
        {
            return LocalRedirect(SafeReturnUrl());
        }

        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        var customer = await _unitOfWork.Customers.GetRegisteredByEmailAsync(Input.Email);

        // Same message for "no such account" and "wrong password" so the form can't be used to
        // find out which e-mails have accounts.
        if (customer?.PasswordHash is null || !Pbkdf2PasswordHasher.Verify(Input.Password, customer.PasswordHash))
        {
            ModelState.AddModelError(string.Empty, "Incorrect e-mail or password.");
            return Page();
        }

        await HttpContext.SignInCustomerAsync(customer);
        return LocalRedirect(SafeReturnUrl());
    }

    private string SafeReturnUrl() => Url.IsLocalUrl(ReturnUrl) ? ReturnUrl! : "/";

    public class LoginInputModel
    {
        [Required(ErrorMessage = "E-mail is required.")]
        [EmailAddress]
        [Display(Name = "E-mail")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Password is required.")]
        [DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;
    }
}
