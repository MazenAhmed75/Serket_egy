namespace ECommerceStore.Core.Interfaces;

/// <summary>
/// Sends e-mail: alerts to the store owner (new order, payment verification needed, etc.) and messages to
/// customers (order receipt, back-in-stock reminder). Implementations must never let a delivery failure
/// bubble up and break the calling use case. If e-mail is not configured, sending is skipped.
/// </summary>
public interface IEmailNotifier
{
    /// <summary>Alert to the store owner (the address configured as Email:ToAddress).</summary>
    Task SendOrderNotificationAsync(string subject, string body, CancellationToken cancellationToken = default);

    /// <summary>A message to one customer. Returns true when it was handed to the mail server.</summary>
    Task<bool> SendToCustomerAsync(string toAddress, string subject, string body, CancellationToken cancellationToken = default);
}
