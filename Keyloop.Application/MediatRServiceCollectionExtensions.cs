using Keyloop.Application.Behaviors;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace Keyloop.Application;

public static class MediatRServiceCollectionExtensions
{
    public static IServiceCollection AddApplicationRequestPipeline(this IServiceCollection services)
    {
        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(typeof(MediatRServiceCollectionExtensions).Assembly);
            cfg.AddOpenBehavior(typeof(LoggingBehavior<,>));
            cfg.AddOpenBehavior(typeof(ValidationBehavior<,>));
            cfg.AddOpenBehavior(typeof(TransactionBehavior<,>));
            cfg.AddOpenBehavior(typeof(AuditBehavior<,>));
        });

        return services;
    }
}
