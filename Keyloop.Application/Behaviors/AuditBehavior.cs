using Keyloop.Application.Abstractions.Auditing;
using Keyloop.Application.Messaging;
using MediatR;

namespace Keyloop.Application.Behaviors;

public sealed class AuditBehavior<TRequest, TResponse>(IAuditLogCollector auditLogCollector)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var response = await next();

        if (request is ITransactionalRequest)
            auditLogCollector.CapturePendingChanges();

        return response;
    }
}
