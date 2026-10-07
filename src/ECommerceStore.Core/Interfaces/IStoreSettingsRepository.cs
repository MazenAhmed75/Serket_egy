using ECommerceStore.Core.Entities;

namespace ECommerceStore.Core.Interfaces;

public interface IStoreSettingsRepository : IRepository<StoreSettings>
{
    /// <summary>The single settings row; created (tracked, saved with the next SaveChanges) with defaults if missing.</summary>
    Task<StoreSettings> GetCurrentAsync();
}
