using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi.Models;

namespace Keyloop.API.OpenApi;

/// <summary>
/// Registers the JWT bearer scheme so the Swagger UI can send the token
/// returned by the login endpoint.
/// </summary>
internal sealed class BearerSecurityDocumentTransformer : IOpenApiDocumentTransformer
{
    public Task TransformAsync(
        OpenApiDocument document,
        OpenApiDocumentTransformerContext context,
        CancellationToken cancellationToken)
    {
        document.Components ??= new OpenApiComponents();
        document.Components.SecuritySchemes ??= new Dictionary<string, OpenApiSecurityScheme>();
        document.Components.SecuritySchemes["Bearer"] = new OpenApiSecurityScheme
        {
            Type = SecuritySchemeType.Http,
            Scheme = "bearer",
            BearerFormat = "JWT",
            Description = "Access token from POST /api/v1/authentication/login."
        };

        return Task.CompletedTask;
    }
}

/// <summary>
/// Replaces the version route parameter with the current v1 segment so the
/// generated document can be called directly from Swagger without typing a
/// version value.
/// </summary>
internal sealed class ApiVersionPathTransformer : IOpenApiDocumentTransformer
{
    public Task TransformAsync(
        OpenApiDocument document,
        OpenApiDocumentTransformerContext context,
        CancellationToken cancellationToken)
    {
        foreach (var path in document.Paths.ToList())
        {
            if (!path.Key.Contains("{version}", StringComparison.OrdinalIgnoreCase))
                continue;

            document.Paths.Remove(path.Key);
            foreach (var operation in path.Value.Operations.Values)
            {
                operation.Parameters = operation.Parameters?
                    .Where(parameter => parameter.In != ParameterLocation.Path || parameter.Name != "version")
                    .ToList();
            }

            document.Paths[path.Key.Replace("{version}", "1", StringComparison.OrdinalIgnoreCase)] = path.Value;
        }

        return Task.CompletedTask;
    }
}

/// <summary>
/// Adds the bearer requirement only to endpoints that require authorization,
/// leaving login and registration callable without a token.
/// </summary>
internal sealed class AuthorizedOperationTransformer : IOpenApiOperationTransformer
{
    public Task TransformAsync(
        OpenApiOperation operation,
        OpenApiOperationTransformerContext context,
        CancellationToken cancellationToken)
    {
        var metadata = context.Description.ActionDescriptor.EndpointMetadata;
        var allowsAnonymous = metadata.OfType<IAllowAnonymous>().Any();
        var requiresAuthorization = metadata.OfType<IAuthorizeData>().Any();

        if (!allowsAnonymous && requiresAuthorization)
        {
            operation.Security ??= [];
            operation.Security.Add(new OpenApiSecurityRequirement
            {
                [new OpenApiSecurityScheme
                {
                    Reference = new OpenApiReference
                    {
                        Type = ReferenceType.SecurityScheme,
                        Id = "Bearer"
                    }
                }] = Array.Empty<string>()
            });
        }

        return Task.CompletedTask;
    }
}
