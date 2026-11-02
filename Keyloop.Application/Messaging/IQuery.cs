using MediatR;

namespace Keyloop.Application.Messaging;

/// <summary>Marks a read-only request. Queries do not start a database transaction.</summary>
public interface IQuery<out TResponse> : IRequest<TResponse>;
