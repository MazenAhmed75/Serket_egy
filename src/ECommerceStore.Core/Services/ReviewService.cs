using ECommerceStore.Core.Entities;
using ECommerceStore.Core.Interfaces;

namespace ECommerceStore.Core.Services;

public sealed record ReviewResult(bool Success, string? Error)
{
    public static ReviewResult Ok() => new(true, null);

    public static ReviewResult Fail(string error) => new(false, error);
}

/// <summary>The rules for leaving a review: a logged-in customer, 1-5 stars, one review per product.</summary>
public class ReviewService
{
    private readonly IUnitOfWork _unitOfWork;

    public ReviewService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<ReviewResult> SubmitAsync(Guid productId, Guid customerId, int rating, string? comment, bool anonymous)
    {
        if (rating is < 1 or > 5)
        {
            return ReviewResult.Fail("Please choose a rating from 1 to 5 stars.");
        }

        var trimmed = string.IsNullOrWhiteSpace(comment) ? null : comment.Trim();
        if (trimmed is not null && trimmed.Length > Review.MaxCommentLength)
        {
            return ReviewResult.Fail($"Please keep your review under {Review.MaxCommentLength} characters.");
        }

        var product = await _unitOfWork.Products.GetByIdAsync(productId);
        if (product is null || !product.IsActive)
        {
            return ReviewResult.Fail("This product is not available.");
        }

        var customer = await _unitOfWork.Customers.GetByIdAsync(customerId);
        if (customer is null)
        {
            return ReviewResult.Fail("Please log in to leave a review.");
        }

        if (await _unitOfWork.Reviews.HasReviewedAsync(productId, customerId))
        {
            return ReviewResult.Fail("You have already reviewed this product.");
        }

        await _unitOfWork.Reviews.AddAsync(new Review
        {
            Id = Guid.NewGuid(),
            ProductId = productId,
            CustomerId = customerId,
            Rating = rating,
            Comment = trimmed,
            IsAnonymous = anonymous,
            AuthorName = customer.FullName,
            CreatedAt = DateTime.UtcNow
        });
        await _unitOfWork.SaveChangesAsync();

        return ReviewResult.Ok();
    }
}
