using ECommerceStore.Core.Entities;
using ECommerceStore.Core.Interfaces;

namespace ECommerceStore.Core.Services;

public sealed record PromoValidationResult(bool IsValid, string? Error, PromoCode? Promo, bool RequiresLogin = false)
{
    public static PromoValidationResult Ok(PromoCode promo) => new(true, null, promo);

    public static PromoValidationResult Fail(string error, bool requiresLogin = false) => new(false, error, null, requiresLogin);
}

/// <summary>Checks whether a typed promo code can be used by the current customer.</summary>
public class PromoCodeService
{
    private readonly IUnitOfWork _unitOfWork;

    public PromoCodeService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public static string Normalize(string? code) => (code ?? string.Empty).Trim().ToUpperInvariant();

    public async Task<PromoValidationResult> ValidateAsync(string? code, Guid? customerId)
    {
        var normalized = Normalize(code);
        if (normalized.Length == 0)
        {
            return PromoValidationResult.Fail("Enter a promo code.");
        }

        if (customerId is null)
        {
            return PromoValidationResult.Fail("Please log in to use a promo code.", requiresLogin: true);
        }

        var promo = await _unitOfWork.PromoCodes.GetByCodeAsync(normalized);
        if (promo is null || !promo.IsActive)
        {
            return PromoValidationResult.Fail("This promo code is not valid.");
        }

        if (await _unitOfWork.PromoCodes.HasRedeemedAsync(promo.Id, customerId.Value))
        {
            return PromoValidationResult.Fail("You have already used this promo code.");
        }

        return PromoValidationResult.Ok(promo);
    }
}
