using Microsoft.AspNetCore.DataProtection;

namespace ECommerceStore.Web.Services;

/// <summary>
/// Order numbers are short and could be guessed, so the confirmation page isn't opened with the plain order number.
/// After an order is placed the customer is sent to a link carrying a tamper-proof, expiring token that only this
/// site can create. Without a valid token the page shows nothing.
/// </summary>
public class OrderConfirmationLinks
{
    private static readonly TimeSpan Lifetime = TimeSpan.FromDays(7);

    private readonly ITimeLimitedDataProtector _protector;

    public OrderConfirmationLinks(IDataProtectionProvider provider)
    {
        _protector = provider.CreateProtector("Serket.OrderConfirmation").ToTimeLimitedDataProtector();
    }

    public string CreateToken(string orderNumber) => _protector.Protect(orderNumber, Lifetime);

    /// <summary>The order number inside a valid token, or null when the token is missing, altered or expired.</summary>
    public string? ReadToken(string? token)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            return null;
        }

        try
        {
            return _protector.Unprotect(token);
        }
        catch (System.Security.Cryptography.CryptographicException)
        {
            return null;
        }
    }
}
