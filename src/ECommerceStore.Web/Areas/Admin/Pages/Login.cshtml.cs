using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using ECommerceStore.Infrastructure.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.RateLimiting;

namespace ECommerceStore.Web.Areas.Admin.Pages;

[AllowAnonymous]
[EnableRateLimiting("auth")]
public class LoginModel : PageModel
{
    private readonly IConfiguration _configuration;

    public LoginModel(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    [BindProperty]
    public LoginInputModel Input { get; set; } = new();

    public string? ErrorMessage { get; private set; }

    public void OnGet()
    {
    }

    public async Task<IActionResult> OnPostAsync(string? returnUrl = null)
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        var configuredUsername = _configuration["Admin:Username"];
        var configuredHash = _configuration["Admin:PasswordHash"];

        var isValid =
            !string.IsNullOrEmpty(configuredUsername) &&
            !string.IsNullOrEmpty(configuredHash) &&
            string.Equals(Input.Username, configuredUsername, StringComparison.OrdinalIgnoreCase) &&
            Pbkdf2PasswordHasher.Verify(Input.Password, configuredHash);

        if (!isValid)
        {
            ErrorMessage = "Incorrect username or password.";
            return Page();
        }

        var claims = new List<Claim> { new(ClaimTypes.Name, configuredUsername!) };
        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);

        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(identity));

        var safeUrl = Url.IsLocalUrl(returnUrl) ? returnUrl : "/Admin";
        return LocalRedirect(safeUrl);
    }

    public class LoginInputModel
    {
        [Required]
        public string Username { get; set; } = string.Empty;

        [Required]
        [DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;
    }
}
