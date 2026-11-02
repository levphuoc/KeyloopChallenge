using FluentValidation;
using Keyloop.Domain.Exceptions;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Keyloop.API.Handlers;

public sealed class GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        if (exception is OperationCanceledException && httpContext.RequestAborted.IsCancellationRequested)
            return false;

        var problem = Map(httpContext, exception);
        httpContext.Response.StatusCode = problem.Status ?? StatusCodes.Status500InternalServerError;
        httpContext.Response.ContentType = "application/problem+json";
        await httpContext.Response.WriteAsJsonAsync(problem, cancellationToken);
        return true;
    }

    private ProblemDetails Map(HttpContext httpContext, Exception exception)
    {
        var (status, title, detail, extensions) = exception switch
        {
            ValidationException ex => (
                StatusCodes.Status400BadRequest,
                "Validation failed",
                "One or more validation errors occurred.",
                ToValidationExtensions(ex)),

            NotFoundException ex => (
                StatusCodes.Status404NotFound,
                "Resource not found",
                ex.Message,
                WithCode(ex)),

            ForbiddenException ex => (
                StatusCodes.Status403Forbidden,
                "Forbidden",
                ex.Message,
                WithCode(ex)),

            SlotUnavailableException ex => (
                StatusCodes.Status409Conflict,
                "Slot unavailable",
                ex.Message,
                WithCode(ex)),

            ConflictException ex => (
                StatusCodes.Status409Conflict,
                "Conflict",
                ex.Message,
                WithCode(ex)),

            DomainException ex => (
                StatusCodes.Status400BadRequest,
                "Business rule violated",
                ex.Message,
                WithCode(ex)),

            UnauthorizedAccessException ex => (
                StatusCodes.Status401Unauthorized,
                "Unauthorized",
                ex.Message,
                null),

            DbUpdateConcurrencyException ex => LogAndMap(
                ex,
                StatusCodes.Status409Conflict,
                "Concurrency conflict",
                "The resource was modified by another request. Please retry."),

            DbUpdateException ex => LogAndMap(
                ex,
                StatusCodes.Status400BadRequest,
                "Database update failed",
                "The request could not be saved."),

            _ => LogAndMap(
                exception,
                StatusCodes.Status500InternalServerError,
                "Internal server error",
                "An unexpected error occurred.")
        };

        var problem = new ProblemDetails
        {
            Status = status,
            Title = title,
            Detail = detail,
            Instance = httpContext.Request.Path,
            Type = $"https://httpstatuses.io/{status}"
        };

        problem.Extensions["traceId"] = httpContext.TraceIdentifier;
        if (extensions is not null)
        {
            foreach (var (key, value) in extensions)
                problem.Extensions[key] = value;
        }

        return problem;
    }

    private (int Status, string Title, string Detail, Dictionary<string, object?>? Extensions) LogAndMap(
        Exception exception,
        int status,
        string title,
        string detail)
    {
        if (status >= StatusCodes.Status500InternalServerError)
            logger.LogError(exception, "Unhandled exception");
        else
            logger.LogWarning(exception, title);

        return (status, title, detail, null);
    }

    private static Dictionary<string, object?> ToValidationExtensions(ValidationException exception)
    {
        var errors = exception.Errors
            .GroupBy(error => error.PropertyName)
            .ToDictionary(group => group.Key, group => group.Select(error => error.ErrorMessage).ToArray());

        return new Dictionary<string, object?>
        {
            ["errors"] = errors,
            ["code"] = "validation_failed"
        };
    }

    private static Dictionary<string, object?> WithCode(DomainException exception) =>
        new() { ["code"] = exception.Code };
}
