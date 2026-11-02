using System.Diagnostics;
using FluentValidation;
using Keyloop.Domain.Exceptions;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Keyloop.Application.Behaviors;

public sealed class LoggingBehavior<TRequest, TResponse>(ILogger<LoggingBehavior<TRequest, TResponse>> logger)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var requestName = typeof(TRequest).Name;
        var stopwatch = Stopwatch.StartNew();
        logger.LogInformation("Handling {RequestName}", requestName);

        try
        {
            var response = await next();
            logger.LogInformation("Handled {RequestName} in {ElapsedMilliseconds} ms", requestName, stopwatch.ElapsedMilliseconds);
            return response;
        }
        catch (ValidationException)
        {
            logger.LogWarning(
                "Rejected {RequestName} after {ElapsedMilliseconds} ms with validation errors",
                requestName,
                stopwatch.ElapsedMilliseconds);
            throw;
        }
        catch (DomainException exception)
        {
            logger.LogWarning(
                "Rejected {RequestName} after {ElapsedMilliseconds} ms with {Code}",
                requestName,
                stopwatch.ElapsedMilliseconds,
                exception.Code);
            throw;
        }
        catch (UnauthorizedAccessException)
        {
            logger.LogWarning(
                "Rejected {RequestName} after {ElapsedMilliseconds} ms due to invalid credentials",
                requestName,
                stopwatch.ElapsedMilliseconds);
            throw;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Failed {RequestName} after {ElapsedMilliseconds} ms", requestName, stopwatch.ElapsedMilliseconds);
            throw;
        }
    }
}
