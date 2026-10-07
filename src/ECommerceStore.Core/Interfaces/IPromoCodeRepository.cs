using ECommerceStore.Core.Entities;

namespace ECommerceStore.Core.Interfaces;

public interface IPromoCodeRepository : IRepository<PromoCode>
{
    /// <summary>Case-insensitive lookup.</summary>
    Task<PromoCode?> GetByCodeAsync(string code);

    Task<bool> HasRedeemedAsync(Guid promoCodeId, Guid customerId);

    Task<int> CountRedemptionsAsync(Guid promoCodeId);

    /// <summary>All codes, newest first, each with its redemption count available via <see cref="CountRedemptionsAsync"/>.</summary>
    Task<IReadOnlyList<PromoCode>> GetAllOrderedAsync();
}
