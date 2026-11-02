using Keyloop.Application.Abstractions;
using Keyloop.Application.Abstractions.Persistence;
using Keyloop.Application.Abstractions.Scheduler;
using Keyloop.Infrastructure.Identity;
using Keyloop.Infrastructure.Persistence.Context;
using Keyloop.Infrastructure.Persistence.Interceptors;
using Keyloop.Infrastructure.Persistence.Queries;
using Keyloop.Infrastructure.Persistence.Repositories;
using Keyloop.Infrastructure.Persistence.Scheduler;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Keyloop.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton(TimeProvider.System);
        services.TryAddScoped<ICurrentUser, SystemCurrentUser>();
        services.AddScoped<AuditableEntityInterceptor>();

        services.AddDbContext<AppDbContext>((provider, options) =>
        {
            options.UseSqlServer(
                configuration.GetConnectionString("DefaultConnection"),
                sql => sql.EnableRetryOnFailure());
            options.AddInterceptors(provider.GetRequiredService<AuditableEntityInterceptor>());
        });

        services.AddScoped(typeof(IRepository<,>), typeof(Repository<,>));
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<ISqlQueryExecutor, DapperSqlQueryExecutor>();
        services.AddScoped<IAppointmentScheduler, AppointmentScheduler>();

        return services;
    }
}
