using Keyloop.Application.Abstractions.Persistence;
using Keyloop.Domain.Entities;
using Keyloop.Infrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore.Storage;

namespace Keyloop.Infrastructure.Persistence.Repositories;

public sealed class UnitOfWork(AppDbContext context) : IUnitOfWork
{
    private readonly Dictionary<string, object> _repositories = [];
    private IDbContextTransaction? _transaction;

    public IRepository<TEntity, TKey> Repository<TEntity, TKey>()
        where TEntity : class, IEntity<TKey>
    {
        var key = $"{typeof(TEntity).FullName}:{typeof(TKey).FullName}";
        if (_repositories.TryGetValue(key, out var existing))
            return (IRepository<TEntity, TKey>)existing;

        var repository = new Repository<TEntity, TKey>(context);
        _repositories[key] = repository;
        return repository;
    }

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        => context.SaveChangesAsync(cancellationToken);

    public async Task BeginTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (_transaction is not null)
            return;

        _transaction = await context.Database.BeginTransactionAsync(cancellationToken);
    }

    public async Task CommitTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (_transaction is null)
            return;

        await context.SaveChangesAsync(cancellationToken);
        await _transaction.CommitAsync(cancellationToken);
        await DisposeTransactionAsync();
    }

    public async Task RollbackTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (_transaction is null)
            return;

        await _transaction.RollbackAsync(cancellationToken);
        await DisposeTransactionAsync();
    }

    public async ValueTask DisposeAsync()
    {
        await DisposeTransactionAsync();
        GC.SuppressFinalize(this);
    }

    private async Task DisposeTransactionAsync()
    {
        if (_transaction is null)
            return;

        await _transaction.DisposeAsync();
        _transaction = null;
    }
}
