using ECommerceStore.Core.Entities;
using ECommerceStore.Core.Interfaces;
using ECommerceStore.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ECommerceStore.Infrastructure.Repositories;

public class StoreSettingsRepository : Repository<StoreSettings>, IStoreSettingsRepository
{
    public StoreSettingsRepository(AppDbContext context) : base(context)
    {
    }

    public async Task<StoreSettings> GetCurrentAsync()
    {
        var settings = await DbSet.FirstOrDefaultAsync();
        if (settings is null)
        {
            settings = new StoreSettings();
            await DbSet.AddAsync(settings);
        }

        return settings;
    }
}
