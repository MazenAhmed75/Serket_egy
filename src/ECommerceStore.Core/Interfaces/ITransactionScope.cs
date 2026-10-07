namespace ECommerceStore.Core.Interfaces;

/// <summary>
/// A database transaction. Everything saved between <see cref="IUnitOfWork.BeginTransactionAsync"/> and
/// <see cref="CommitAsync"/> is kept together or thrown away together. Disposing without committing rolls back.
/// </summary>
public interface ITransactionScope : IAsyncDisposable
{
    Task CommitAsync(CancellationToken cancellationToken = default);

    /// <summary>Undo everything now (needed before running more queries after a failed save, because PostgreSQL refuses further commands in a failed transaction).</summary>
    Task RollbackAsync(CancellationToken cancellationToken = default);
}
