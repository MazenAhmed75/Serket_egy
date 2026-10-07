using ECommerceStore.Core.DTOs;
using ECommerceStore.Core.Entities;

namespace ECommerceStore.Core.Interfaces;

public interface IReviewRepository : IRepository<Review>
{
    /// <summary>The product's reviews that are not hidden, newest first.</summary>
    Task<IReadOnlyList<Review>> GetVisibleForProductAsync(Guid productId);

    /// <summary>Average rating and count per product (hidden reviews are not counted). Products with no reviews are absent.</summary>
    Task<IReadOnlyDictionary<Guid, RatingSummary>> GetSummariesAsync();

    Task<bool> HasReviewedAsync(Guid productId, Guid customerId);

    /// <summary>Every review with its product loaded, newest first (Admin list).</summary>
    Task<IReadOnlyList<Review>> GetAllWithProductAsync();
}
