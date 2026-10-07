using System.Linq.Expressions;

namespace ECommerceStore.Core.Interfaces;

/// <summary>
/// Generic, persistence-ignorant repository contract shared by every entity-specific repository.
/// </summary>
public interface IRepository<T> where T : class
{
    Task<T?> GetByIdAsync(Guid id);

    Task<IReadOnlyList<T>> FindAsync(Expression<Func<T, bool>> predicate);

    Task<T?> SingleOrDefaultAsync(Expression<Func<T, bool>> predicate);

    Task AddAsync(T entity);

    void Update(T entity);

    void Remove(T entity);
}
