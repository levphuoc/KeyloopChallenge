using Keyloop.Application.Abstractions.Authentication;
using Keyloop.Application.Messaging;
using MediatR;

namespace Keyloop.Application.Features.Authentication;

public sealed record RegisterUserCommand(string Email, string Password, string DisplayName) : ICommand<AuthToken>;

public sealed class RegisterUserCommandHandler(IAuthenticationService authenticationService)
    : IRequestHandler<RegisterUserCommand, AuthToken>
{
    public Task<AuthToken> Handle(RegisterUserCommand request, CancellationToken cancellationToken) =>
        authenticationService.RegisterAsync(new RegisterUserInput(request.Email, request.Password, request.DisplayName), cancellationToken);
}
