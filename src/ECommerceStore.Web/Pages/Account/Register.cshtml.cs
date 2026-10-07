using System.ComponentModel.DataAnnotations;
using ECommerceStore.Core.Entities;
using ECommerceStore.Core.Interfaces;
using ECommerceStore.Infrastructure.Services;
using ECommerceStore.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.RateLimiting;

namespace ECommerceStore.Web.Pages.Account;

[EnableRateLimiting("auth")]
public class RegisterModel : PageModel
{
    private readonly IUnitOfWork _unitOfWork;

    public RegisterModel(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    [BindProperty]
    public RegisterInputModel Input { get; set; } = new();

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

        var email = Input.Email.Trim().ToLowerInvariant();
        var phone = Input.PhoneNumber.Trim();

        if (await _unitOfWork.Customers.GetRegisteredByEmailAsync(email) is not null)
        {
            ModelState.AddModelError("Input.Email", "An account with this e-mail already exists. Try logging in instead.");
            return Page();
        }

        if (await _unitOfWork.Customers.GetRegisteredByPhoneNumberAsync(phone) is not null)
        {
            ModelState.AddModelError("Input.PhoneNumber", "An account already uses this phone number.");
            return Page();
        }

        var customer = new Customer { Id = Guid.NewGuid(), PhoneNumber = phone };
        await _unitOfWork.Customers.AddAsync(customer);

        customer.FullName = Input.FullName.Trim();
        customer.Email = email;
        customer.PasswordHash = Pbkdf2PasswordHasher.Hash(Input.Password);

        await _unitOfWork.SaveChangesAsync();
        await HttpContext.SignInCustomerAsync(customer);

        return LocalRedirect(SafeReturnUrl());
    }

    private string SafeReturnUrl() => Url.IsLocalUrl(ReturnUrl) ? ReturnUrl! : "/";

    public class RegisterInputModel
    {
        [Required(ErrorMessage = "Full name is required.")]
        [StringLength(150)]
        [Display(Name = "Full name")]
        public string FullName { get; set; } = string.Empty;

        [Required(ErrorMessage = "E-mail is required.")]
        [EmailAddress]
        [StringLength(200)]
        [Display(Name = "E-mail")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Phone number is required.")]
        [Phone]
        [StringLength(20)]
        [Display(Name = "Phone number")]
        public string PhoneNumber { get; set; } = string.Empty;

        [Required(ErrorMessage = "Please choose a password.")]
        [StringLength(100, MinimumLength = 8, ErrorMessage = "Password must be at least 8 characters.")]
        [DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;

        [Required(ErrorMessage = "Please confirm your password.")]
        [Compare(nameof(Password), ErrorMessage = "The two passwords do not match.")]
        [DataType(DataType.Password)]
        [Display(Name = "Confirm password")]
        public string ConfirmPassword { get; set; } = string.Empty;
    }
}
