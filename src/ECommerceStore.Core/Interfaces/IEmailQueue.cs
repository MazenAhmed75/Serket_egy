namespace ECommerceStore.Core.Interfaces;

/// <summary>A message waiting to be sent. <see cref="To"/> is null for a message to the store owner.</summary>
public sealed record EmailMessage(string? To, string Subject, string Body);

/// <summary>
/// Hands e-mails to a background sender and returns immediately, so a slow or unreachable mail server can never
/// delay (or break) the page that placed the order.
/// </summary>
public interface IEmailQueue
{
    void Enqueue(EmailMessage message);
}
