using Keyloop.Infrastructure.Persistence.Context;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace Keyloop.API.HostedServices;

/// <summary>
/// Applies EF Core migrations and tolerates a database that already exists.
/// A database can be present in SQL Server metadata but impossible to open when
/// its physical files were removed or moved (SQL Server error 1801 on CREATE).
/// In that case the unusable database is dropped and recreated.
/// </summary>
internal static class DatabaseMigrator
{
    private const int DatabaseAlreadyExists = 1801;

    public static async Task MigrateAsync(
        AppDbContext context,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        try
        {
            await context.Database.MigrateAsync(cancellationToken);
        }
        catch (SqlException exception) when (exception.Number == DatabaseAlreadyExists)
        {
            var database = context.Database.GetDbConnection().Database;

            if (await context.Database.CanConnectAsync(cancellationToken))
            {
                logger.LogWarning(
                    "Database '{Database}' already exists; skipping creation and applying pending migrations.",
                    database);
                await context.Database.MigrateAsync(cancellationToken);
                return;
            }

            logger.LogWarning(
                "Database '{Database}' exists but cannot be opened (for example, its files are missing). " +
                "Dropping and recreating it.",
                database);

            await DropAsync(context, logger, cancellationToken);
            await context.Database.MigrateAsync(cancellationToken);
        }
    }

    private static async Task DropAsync(
        AppDbContext context,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        var connection = context.Database.GetDbConnection();
        var database = connection.Database;
        var masterConnectionString = new SqlConnectionStringBuilder(connection.ConnectionString)
        {
            InitialCatalog = "master"
        }.ConnectionString;

        SqlConnection.ClearAllPools();

        await using var master = new SqlConnection(masterConnectionString);
        await master.OpenAsync(cancellationToken);

        var literal = database.Replace("'", "''");
        var quoted = database.Replace("]", "]]");
        await using var command = master.CreateCommand();
        command.CommandText = $"""
            IF DB_ID(N'{literal}') IS NOT NULL
            BEGIN
                ALTER DATABASE [{quoted}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
                DROP DATABASE [{quoted}];
            END
            """;

        try
        {
            await command.ExecuteNonQueryAsync(cancellationToken);
        }
        catch (SqlException exception)
        {
            // SQL Server can report file-activation errors (5120/5181) while
            // still completing the DROP. Only fail if the database is still there.
            if (await ExistsAsync(master, database, cancellationToken))
            {
                throw new InvalidOperationException(
                    $"Database '{database}' exists but could not be dropped. " +
                    "Close other connections and run: dotnet ef database drop --force",
                    exception);
            }

            logger.LogDebug(exception, "Database '{Database}' was dropped after reporting file errors.", database);
        }
    }

    private static async Task<bool> ExistsAsync(
        SqlConnection master,
        string database,
        CancellationToken cancellationToken)
    {
        await using var command = master.CreateCommand();
        command.CommandText = "SELECT CASE WHEN DB_ID(@name) IS NULL THEN 0 ELSE 1 END";
        command.Parameters.AddWithValue("@name", database);
        return (int)(await command.ExecuteScalarAsync(cancellationToken))! == 1;
    }
}
