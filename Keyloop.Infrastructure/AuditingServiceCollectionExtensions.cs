using Keyloop.Application.Abstractions.Auditing;
using Keyloop.Infrastructure.Persistence.Auditing;
using Microsoft.Extensions.DependencyInjection;

namespace Keyloop.Infrastructure;

public static class AuditingServiceCollectionExtensions
{
    public static IServiceCollection AddPersistenceAuditing(this IServiceCollection services)
    {
        services.AddScoped<IAuditLogCollector, AuditLogCollector>();
        return services;
    }
}
