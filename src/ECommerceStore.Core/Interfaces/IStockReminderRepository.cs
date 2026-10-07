using ECommerceStore.Core.Entities;

namespace ECommerceStore.Core.Interfaces;

public interface IStockReminderRepository : IRepository<StockReminder>
{
    /// <summary>True when this e-mail is already waiting for this exact colour and size.</summary>
    Task<bool> ExistsPendingAsync(Guid productId, Guid? productColorId, string size, string email);

    /// <summary>Reminders for the product that have not been e-mailed yet.</summary>
    Task<IReadOnlyList<StockReminder>> GetPendingForProductAsync(Guid productId);
}
