using Keyloop.Infrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace Keyloop.Infrastructure.Persistence.Factories;

/// <summary>Creates a context lease for workers and jobs that execute outside an HTTP scope.</summary>
public static class AppDbContextFactoryExtensions
{
    public static async ValueTask<DbContextLease> CreateLeaseAsync(
        this IDbContextFactory<AppDbContext> factory,
        CancellationToken cancellationToken = default)
        => new(await factory.CreateDbContextAsync(cancellationToken));
}

/// <summary>Use with <c>await using</c> to guarantee disposal of a factory-created context.</summary>
public sealed class DbContextLease(AppDbContext context) : IAsyncDisposable
{
    public AppDbContext Context { get; } = context;
    public ValueTask DisposeAsync() => Context.DisposeAsync();
}
