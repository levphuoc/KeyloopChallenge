using System.Data;
using Dapper;
using Keyloop.Application.Abstractions.Persistence;
using Keyloop.Infrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace Keyloop.Infrastructure.Persistence.Queries;

internal sealed class DapperSqlQueryExecutor(AppDbContext context) : ISqlQueryExecutor
{
    public async Task<IReadOnlyList<T>> QueryAsync<T>(
        string sql,
        object? parameters = null,
        CancellationToken cancellationToken = default)
    {
        var connection = context.Database.GetDbConnection();
        var shouldClose = connection.State != ConnectionState.Open;
        if (shouldClose)
            await connection.OpenAsync(cancellationToken);

        try
        {
            var command = new CommandDefinition(sql, parameters, cancellationToken: cancellationToken);
            var rows = await connection.QueryAsync<T>(command);
            return rows.AsList();
        }
        finally
        {
            if (shouldClose)
                await connection.CloseAsync();
        }
    }

    public async Task<T?> QuerySingleOrDefaultAsync<T>(
        string sql,
        object? parameters = null,
        CancellationToken cancellationToken = default)
    {
        var connection = context.Database.GetDbConnection();
        var shouldClose = connection.State != ConnectionState.Open;
        if (shouldClose)
            await connection.OpenAsync(cancellationToken);

        try
        {
            var command = new CommandDefinition(sql, parameters, cancellationToken: cancellationToken);
            return await connection.QuerySingleOrDefaultAsync<T>(command);
        }
        finally
        {
            if (shouldClose)
                await connection.CloseAsync();
        }
    }
}
