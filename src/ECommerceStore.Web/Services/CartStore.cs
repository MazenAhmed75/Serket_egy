using System.Security.Cryptography;
using ECommerceStore.Core.Services;
using Microsoft.AspNetCore.DataProtection;

namespace ECommerceStore.Web.Services;

/// <summary>
/// Keeps the shopper's cart in a cookie that is encrypted and signed by the site, so it can't be read or altered in the
/// browser. Only what was chosen is stored (never prices), and everything is checked again against the database when it is used.
/// If the site's keys change (for example after a restart on a host with a temporary disk) the cookie can't be read and the cart is simply empty.
/// </summary>
public class CartStore
{
    private const string CookieName = ".Serket.Cart";

    private readonly IDataProtector _protector;
    private readonly bool _secureOnly;

    public CartStore(IDataProtectionProvider provider, IWebHostEnvironment environment)
    {
        _protector = provider.CreateProtector("Serket.Cart");
        _secureOnly = !environment.IsDevelopment();
    }

    public Cart Read(HttpContext context)
    {
        if (!context.Request.Cookies.TryGetValue(CookieName, out var value) || string.IsNullOrEmpty(value))
        {
            return Cart.Empty;
        }

        try
        {
            return CartSerializer.Deserialize(_protector.Unprotect(value));
        }
        catch (CryptographicException)
        {
            return Cart.Empty;
        }
    }

    public void Write(HttpContext context, Cart cart)
    {
        if (cart.IsEmpty)
        {
            Clear(context);
            return;
        }

        context.Response.Cookies.Append(CookieName, _protector.Protect(CartSerializer.Serialize(cart)), Options(TimeSpan.FromDays(14)));
    }

    public void Clear(HttpContext context) => context.Response.Cookies.Delete(CookieName, Options(null));

    private CookieOptions Options(TimeSpan? maxAge) => new()
    {
        HttpOnly = true,
        Secure = _secureOnly,
        SameSite = SameSiteMode.Lax,
        IsEssential = true,
        Path = "/",
        MaxAge = maxAge
    };
}
