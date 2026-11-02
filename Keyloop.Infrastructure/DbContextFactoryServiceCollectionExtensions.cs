using Keyloop.Infrastructure.Persistence.Context;
using Keyloop.Infrastructure.Persistence.Interceptors;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Keyloop.Infrastructure;

public static class DbContextFactoryServiceCollectionExtensions
{
    public static IServiceCollection AddAppDbContextFactory(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddDbContextFactory<AppDbContext>((provider, options) =>
        {
            options.UseSqlServer(
                configuration.GetConnectionString("DefaultConnection"),
                sql => sql.EnableRetryOnFailure());
            options.AddInterceptors(provider.GetRequiredService<AuditableEntityInterceptor>());
        }, ServiceLifetime.Scoped);

        return services;
    }
}
