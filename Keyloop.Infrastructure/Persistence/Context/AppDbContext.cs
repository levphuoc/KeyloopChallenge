using Microsoft.EntityFrameworkCore;

namespace Keyloop.Infrastructure.Persistence.Context;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
        modelBuilder.ApplySoftDeleteQueryFilters();
        modelBuilder.ApplyConcurrencyTokens();
    }
}
