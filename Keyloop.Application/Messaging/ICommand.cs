using MediatR;

namespace Keyloop.Application.Messaging;

/// <summary>Marks a request that changes state and must run in a transaction.</summary>
public interface ICommand<out TResponse> : IRequest<TResponse>, ITransactionalRequest;

public interface ITransactionalRequest;
