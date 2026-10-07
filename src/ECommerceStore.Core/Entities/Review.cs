namespace ECommerceStore.Core.Entities;

/// <summary>A customer's star rating (and optional comment) for a product. One per customer account per product.</summary>
public class Review
{
    public const int MaxCommentLength = 1000;

    public Guid Id { get; set; }

    public Guid ProductId { get; set; }

    public Guid CustomerId { get; set; }

    /// <summary>1 to 5 stars.</summary>
    public int Rating { get; set; }

    public string? Comment { get; set; }

    /// <summary>When true the reviewer's name is not shown publicly (the shop owner can still see who wrote it).</summary>
    public bool IsAnonymous { get; set; }

    /// <summary>The reviewer's full name when they wrote the review.</summary>
    public string AuthorName { get; set; } = string.Empty;

    /// <summary>Set by the shop owner to take a review off the storefront without deleting it.</summary>
    public bool IsHidden { get; set; }

    public DateTime CreatedAt { get; set; }

    // Navigation
    public Product Product { get; set; } = null!;

    public Customer Customer { get; set; } = null!;

    /// <summary>What the storefront shows: "Anonymous", or the first name plus the last initial ("Mazen A.").</summary>
    public string PublicName
    {
        get
        {
            if (IsAnonymous)
            {
                return "Anonymous";
            }

            var parts = AuthorName.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            return parts.Length switch
            {
                0 => "Customer",
                1 => parts[0],
                _ => $"{parts[0]} {char.ToUpperInvariant(parts[^1][0])}."
            };
        }
    }
}
