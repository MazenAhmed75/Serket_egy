using ECommerceStore.Core.Interfaces;
using ECommerceStore.Core.Services;

namespace ECommerceStore.Web.Services;

/// <summary>
/// Every few hours deletes the receipt images of shipped or cancelled orders that are older than the retention period
/// set in Admin &gt; Settings (0 turns it off). It never lets an error escape, because an unhandled exception in a
/// background service would stop the whole site.
/// </summary>
public class ReceiptCleanupWorker : BackgroundService
{
    private static readonly TimeSpan StartupDelay = TimeSpan.FromMinutes(2);
    private static readonly TimeSpan Interval = TimeSpan.FromHours(6);

    private readonly IServiceScopeFactory _scopes;
    private readonly ILogger<ReceiptCleanupWorker> _logger;

    public ReceiptCleanupWorker(IServiceScopeFactory scopes, ILogger<ReceiptCleanupWorker> logger)
    {
        _scopes = scopes;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            await Task.Delay(StartupDelay, stoppingToken);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await RunOnceAsync(stoppingToken);
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    _logger.LogError(ex, "Receipt clean-up failed; it will try again later.");
                }

                await Task.Delay(Interval, stoppingToken);
            }
        }
        catch (OperationCanceledException)
        {
            // The site is shutting down.
        }
    }

    private async Task RunOnceAsync(CancellationToken cancellationToken)
    {
        using var scope = _scopes.CreateScope();

        var settings = await scope.ServiceProvider.GetRequiredService<IUnitOfWork>().Settings.GetCurrentAsync();
        if (settings.ReceiptRetentionDays <= 0)
        {
            return;
        }

        var cutoff = DateTime.UtcNow.AddDays(-settings.ReceiptRetentionDays);
        var result = await scope.ServiceProvider.GetRequiredService<ReceiptCleaner>().DeleteImagesAsync(cutoff, cancellationToken);

        if (result.Deleted > 0 || result.Failed > 0)
        {
            _logger.LogInformation("Receipt clean-up: {Deleted} image(s) deleted, {Failed} failed.", result.Deleted, result.Failed);
        }
    }
}
