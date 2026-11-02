using Keyloop.API.Handlers;
using Keyloop.API.HostedServices;
using Keyloop.API.Services;
using Keyloop.Application;
using Keyloop.Application.Abstractions;
using Keyloop.Infrastructure;
using Asp.Versioning;

namespace Keyloop.API.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddApiServices(
        this IServiceCollection services, IConfiguration configuration)
    {
        services.AddApplication();
        services.AddInfrastructure(configuration);

        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUser, CurrentUser>();

        services.AddExceptionHandler<GlobalExceptionHandler>();
        services.AddProblemDetails();
        services.AddHostedService<DatabaseInitializationHostedService>();

        services.AddControllers();
        services.AddApiVersioning(options =>
        {
            options.DefaultApiVersion = new ApiVersion(1, 0);
            options.AssumeDefaultVersionWhenUnspecified = true;
            options.ReportApiVersions = true;
            options.ApiVersionReader = ApiVersionReader.Combine(
                new UrlSegmentApiVersionReader(),
                new HeaderApiVersionReader("X-Api-Version"));
        }).AddMvc();
        services.AddOpenApi();
        services.AddHealthChecks();

        return services;
    }
}
