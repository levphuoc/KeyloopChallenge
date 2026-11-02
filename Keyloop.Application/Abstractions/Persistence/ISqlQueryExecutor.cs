namespace Keyloop.Application.Abstractions.Persistence;

/// <summary>Read-model access for parameterized SQL queries that do not belong in an aggregate repository.</summary>
public interface ISqlQueryExecutor
{
    Task<IReadOnlyList<T>> QueryAsync<T>(
        string sql,
        object? parameters = null,
        CancellationToken cancellationToken = default);

    Task<T?> QuerySingleOrDefaultAsync<T>(
        string sql,
        object? parameters = null,
        CancellationToken cancellationToken = default);
}
