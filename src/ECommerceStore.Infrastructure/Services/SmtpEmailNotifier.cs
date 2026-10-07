using ECommerceStore.Core.Interfaces;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MimeKit;

namespace ECommerceStore.Infrastructure.Services;

/// <summary>
/// Sends e-mail via SMTP (MailKit): alerts to the store owner and messages to customers. Works with Gmail
/// (with an app password), Outlook, or any provider's SMTP relay: configure Email:* in appsettings. If
/// SmtpHost is missing (or, for owner alerts, ToAddress), sends are skipped with a warning log so the
/// storefront keeps working without e-mail configured.
/// </summary>
public class SmtpEmailNotifier : IEmailNotifier
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<SmtpEmailNotifier> _logger;

    public SmtpEmailNotifier(IConfiguration configuration, ILogger<SmtpEmailNotifier> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public async Task SendOrderNotificationAsync(string subject, string body, CancellationToken cancellationToken = default)
    {
        var toAddress = _configuration["Email:ToAddress"];
        if (string.IsNullOrWhiteSpace(toAddress))
        {
            _logger.LogWarning("Email:ToAddress not configured; skipping owner notification email.");
            return;
        }

        await SendAsync(toAddress, subject, body, cancellationToken);
    }

    public Task<bool> SendToCustomerAsync(string toAddress, string subject, string body, CancellationToken cancellationToken = default) =>
        SendAsync(toAddress, subject, body, cancellationToken);

    private async Task<bool> SendAsync(string toAddress, string subject, string body, CancellationToken cancellationToken)
    {
        var host = _configuration["Email:SmtpHost"];
        if (string.IsNullOrWhiteSpace(host))
        {
            _logger.LogWarning("Email:SmtpHost not configured; skipping email.");
            return false;
        }

        var port = int.TryParse(_configuration["Email:SmtpPort"], out var parsedPort) ? parsedPort : 587;
        var useSsl = !bool.TryParse(_configuration["Email:UseSsl"], out var parsedUseSsl) || parsedUseSsl;
        var username = _configuration["Email:SmtpUsername"];
        var password = _configuration["Email:SmtpPassword"] ?? string.Empty;
        var fromAddress = _configuration["Email:FromAddress"] ?? username ?? toAddress;
        var fromName = _configuration["Email:FromName"] ?? "Serket Store";

        try
        {
            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(fromName, fromAddress));
            message.To.Add(MailboxAddress.Parse(toAddress));
            message.Subject = subject;
            message.Body = new TextPart("plain") { Text = body };

            using var client = new SmtpClient();
            var socketOptions = useSsl ? SecureSocketOptions.StartTls : SecureSocketOptions.Auto;

            await client.ConnectAsync(host, port, socketOptions, cancellationToken);

            if (!string.IsNullOrWhiteSpace(username))
            {
                await client.AuthenticateAsync(username, password, cancellationToken);
            }

            await client.SendAsync(message, cancellationToken);
            await client.DisconnectAsync(true, cancellationToken);
            return true;
        }
        catch (Exception ex)
        {
            // An e-mail failure must never break order placement or stock updates.
            _logger.LogError(ex, "Failed to send email to {Recipient}.", toAddress);
            return false;
        }
    }
}
