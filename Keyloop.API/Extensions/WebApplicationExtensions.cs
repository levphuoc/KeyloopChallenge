using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Serilog;

namespace Keyloop.API.Extensions;

public static class WebApplicationExtensions
{
    public static WebApplication UseApiPipeline(this WebApplication app)
    {
        // Request logging wraps the exception handler so handled domain
        // rejections (for example 409 conflicts) are logged with their real
        // status code instead of a transient 500.
        app.UseSerilogRequestLogging();
        app.UseExceptionHandler();

        if (app.Environment.IsDevelopment())
        {
            app.MapOpenApi();
            app.UseSwaggerUI(options =>
            {
                options.RoutePrefix = "swagger";
                options.SwaggerEndpoint("/openapi/v1.json", "Keyloop API v1");
                options.DocumentTitle = "Keyloop API";
                options.DisplayRequestDuration();
            });
            app.MapGet("/", () => Results.Redirect("/swagger")).ExcludeFromDescription();
        }

        app.UseHttpsRedirection();
        app.UseAuthentication();
        app.UseAuthorization();
        app.MapControllers();

        var readyHealthCheck = new HealthCheckOptions
        {
            Predicate = check => check.Tags.Contains("ready")
        };

        app.MapHealthChecks("/health/live", new HealthCheckOptions { Predicate = _ => false });
        app.MapHealthChecks("/health/ready", readyHealthCheck);
        app.MapHealthChecks("/health", readyHealthCheck);

        return app;
    }
}
