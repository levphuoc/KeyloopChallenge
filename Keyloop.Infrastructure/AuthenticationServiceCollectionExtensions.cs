using Keyloop.Application.Abstractions.Authentication;
using Keyloop.Infrastructure.Authentication;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Keyloop.Infrastructure;

public static class AuthenticationServiceCollectionExtensions
{
    public static IServiceCollection AddApplicationAuthentication(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<JwtOptions>().Bind(configuration.GetSection(JwtOptions.SectionName));
        services.AddOptions<AdminAccountOptions>().Bind(configuration.GetSection(AdminAccountOptions.SectionName));
        services.AddScoped<IAuthenticationService, AuthenticationService>();
        return services;
    }
}
