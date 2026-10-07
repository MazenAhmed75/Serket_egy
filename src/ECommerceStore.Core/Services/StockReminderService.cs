using System.Net.Mail;
using ECommerceStore.Core.Constants;
using ECommerceStore.Core.Entities;
using ECommerceStore.Core.Interfaces;

namespace ECommerceStore.Core.Services;

public sealed record ReminderResult(bool Success, string Message)
{
    public static ReminderResult Ok(string message) => new(true, message);

    public static ReminderResult Fail(string message) => new(false, message);
}

/// <summary>"Remind me when it's back": saves a customer's e-mail for a sold-out colour/size and e-mails it when stock returns.</summary>
public class StockReminderService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IEmailNotifier _emailNotifier;

    public StockReminderService(IUnitOfWork unitOfWork, IEmailNotifier emailNotifier)
    {
        _unitOfWork = unitOfWork;
        _emailNotifier = emailNotifier;
    }

    /// <summary>The product must be loaded with its colours and stock.</summary>
    public async Task<ReminderResult> SubscribeAsync(Product product, Guid? colorId, string? size, string? email)
    {
        var address = (email ?? string.Empty).Trim().ToLowerInvariant();
        if (!MailAddress.TryCreate(address, out var parsed) || parsed.Address != address || !address.Contains('.'))
        {
            return ReminderResult.Fail("Please enter a valid e-mail address.");
        }

        if (!ProductSizes.IsValid(size))
        {
            return ReminderResult.Fail("Please choose a size.");
        }

        var activeColors = product.Colors.Where(c => c.IsActive).ToList();
        if (activeColors.Count > 0 ? activeColors.All(c => c.Id != colorId) : colorId is not null)
        {
            return ReminderResult.Fail("Please choose a colour.");
        }

        if (product.UnitsAvailable(colorId, size!) > 0)
        {
            return ReminderResult.Fail("That size is in stock right now, so you can order it.");
        }

        if (await _unitOfWork.StockReminders.ExistsPendingAsync(product.Id, colorId, size!, address))
        {
            return ReminderResult.Ok("You are already on the list. We will e-mail you when it is back.");
        }

        await _unitOfWork.StockReminders.AddAsync(new StockReminder
        {
            Id = Guid.NewGuid(),
            ProductId = product.Id,
            ProductColorId = colorId,
            Size = size!,
            Email = address,
            CreatedAt = DateTime.UtcNow
        });
        await _unitOfWork.SaveChangesAsync();

        return ReminderResult.Ok("Done! We will e-mail you when it is back in stock.");
    }

    /// <summary>
    /// Call after stock was raised for these colour/size combinations. E-mails everyone waiting for them
    /// and marks those reminders as sent. Returns how many e-mails were sent.
    /// </summary>
    public async Task<int> NotifyRestockedAsync(Product product, IReadOnlyCollection<(Guid? ColorId, string Size)> restocked, string productUrl)
    {
        if (restocked.Count == 0)
        {
            return 0;
        }

        var pending = await _unitOfWork.StockReminders.GetPendingForProductAsync(product.Id);
        var sent = 0;

        foreach (var reminder in pending.Where(r => restocked.Contains((r.ProductColorId, r.Size))))
        {
            var colorName = product.Colors.FirstOrDefault(c => c.Id == reminder.ProductColorId)?.Name;
            var what = colorName is null ? $"size {reminder.Size}" : $"{colorName}, size {reminder.Size}";

            var body =
                $"Good news! {product.Name} ({what}) is back in stock.\n\n" +
                $"You can order it here:\n{productUrl}\n\n" +
                "You are receiving this one-time message because you asked us to remind you.\n\n" +
                "Serket";

            if (await _emailNotifier.SendToCustomerAsync(reminder.Email, $"Back in stock: {product.Name}", body))
            {
                reminder.NotifiedAt = DateTime.UtcNow;
                sent++;
            }
        }

        await _unitOfWork.SaveChangesAsync();
        return sent;
    }
}
