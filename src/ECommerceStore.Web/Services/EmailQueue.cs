using System.Threading.Channels;
using ECommerceStore.Core.Interfaces;

namespace ECommerceStore.Web.Services;

/// <summary>
/// An in-memory waiting line for e-mails. Pages add a message and carry on; <see cref="EmailQueueWorker"/> sends them.
/// If the site restarts before a message is sent, that message is lost (acceptable for receipts and alerts; the order itself is safe in the database).
/// </summary>
public sealed class ChannelEmailQueue : IEmailQueue
{
    private readonly Channel<EmailMessage> _channel = Channel.CreateBounded<EmailMessage>(
        new BoundedChannelOptions(500) { FullMode = BoundedChannelFullMode.DropOldest, SingleReader = true });

    public ChannelReader<EmailMessage> Reader => _channel.Reader;

    public void Enqueue(EmailMessage message) => _channel.Writer.TryWrite(message);
}

/// <summary>Sends queued e-mails one at a time. A failure is logged and never stops the queue or the site.</summary>
public sealed class EmailQueueWorker : BackgroundService
{
    private readonly ChannelEmailQueue _queue;
    private readonly IServiceScopeFactory _scopes;
    private readonly ILogger<EmailQueueWorker> _logger;

    public EmailQueueWorker(ChannelEmailQueue queue, IServiceScopeFactory scopes, ILogger<EmailQueueWorker> logger)
    {
        _queue = queue;
        _scopes = scopes;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            await foreach (var message in _queue.Reader.ReadAllAsync(stoppingToken))
            {
                try
                {
                    using var scope = _scopes.CreateScope();
                    var notifier = scope.ServiceProvider.GetRequiredService<IEmailNotifier>();

                    if (message.To is null)
                    {
                        await notifier.SendOrderNotificationAsync(message.Subject, message.Body, stoppingToken);
                    }
                    else
                    {
                        await notifier.SendToCustomerAsync(message.To, message.Subject, message.Body, stoppingToken);
                    }
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    _logger.LogError(ex, "A queued e-mail could not be sent.");
                }
            }
        }
        catch (OperationCanceledException)
        {
            // The site is shutting down.
        }
    }
}
