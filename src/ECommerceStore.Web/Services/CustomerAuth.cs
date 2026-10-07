using System.Security.Claims;
using ECommerceStore.Core.Entities;
using Microsoft.AspNetCore.Authentication;

namespace ECommerceStore.Web.Services;

/// <summary>
/// Shopper accounts. They use their own cookie scheme, separate from the admin cookie, so a
/// customer login can never open /Admin and the owner's login does not count as a shopper.
/// </summary>
public static class CustomerAuth
{
    public const string Scheme = "CustomerAuth";

    private const string IdKey = "Serket.CustomerId";
    private const string NameKey = "Serket.CustomerName";

    /// <summary>The signed-in shopper's customer id, or null when browsing as a guest.</summary>
    public static Guid? GetCustomerId(this HttpContext context) => context.Items[IdKey] as Guid?;

    public static string? GetCustomerName(this HttpContext context) => context.Items[NameKey] as string;

    public static async Task SignInCustomerAsync(this HttpContext context, Customer customer)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, customer.Id.ToString()),
            new(ClaimTypes.Name, customer.FullName)
        };

        var principal = new ClaimsPrincipal(new ClaimsIdentity(claims, Scheme));
        var properties = new AuthenticationProperties
        {
            IsPersistent = true,
            ExpiresUtc = DateTimeOffset.UtcNow.AddDays(30)
        };

        await context.SignInAsync(Scheme, principal, properties);
    }

    public static Task SignOutCustomerAsync(this HttpContext context) => context.SignOutAsync(Scheme);

    /// <summary>Middleware step: if the shopper cookie is valid, expose the customer id/name for this request.</summary>
    public static async Task LoadCustomerAsync(HttpContext context, Func<Task> next)
    {
        var result = await context.AuthenticateAsync(Scheme);
        if (result.Succeeded && result.Principal is not null)
        {
            var idValue = result.Principal.FindFirstValue(ClaimTypes.NameIdentifier);
            if (Guid.TryParse(idValue, out var customerId))
            {
                context.Items[IdKey] = customerId;
                context.Items[NameKey] = result.Principal.FindFirstValue(ClaimTypes.Name);
            }
        }

        await next();
    }
}
