using System.Linq.Expressions;
using ECommerceStore.Core.Interfaces;
using ECommerceStore.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ECommerceStore.Infrastructure.Repositories;

/// <summary>
/// EF Core implementation of <see cref="IRepository{T}"/> shared by every entity-specific repository.
/// </summary>
public class Repository<T> : IRepository<T> where T : class
{
    protected readonly AppDbContext Context;
    protected readonly DbSet<T> DbSet;

    public Repository(AppDbContext context)
    {
        Context = context;
        DbSet = context.Set<T>();
    }

    public virtual async Task<T?> GetByIdAsync(Guid id) => await DbSet.FindAsync(id);

    public virtual async Task<IReadOnlyList<T>> FindAsync(Expression<Func<T, bool>> predicate) =>
        await DbSet.Where(predicate).ToListAsync();

    public virtual async Task<T?> SingleOrDefaultAsync(Expression<Func<T, bool>> predicate) =>
        await DbSet.SingleOrDefaultAsync(predicate);

    public virtual async Task AddAsync(T entity) => await DbSet.AddAsync(entity);

    public virtual void Update(T entity) => DbSet.Update(entity);

    public virtual void Remove(T entity) => DbSet.Remove(entity);
}
