using Keyloop.Application.Abstractions.Authentication;
using Keyloop.Application.Messaging;
using MediatR;

namespace Keyloop.Application.Features.Authentication;

public sealed record LoginQuery(string Email, string Password) : IQuery<AuthToken>;

public sealed class LoginQueryHandler(IAuthenticationService authenticationService)
    : IRequestHandler<LoginQuery, AuthToken>
{
    public Task<AuthToken> Handle(LoginQuery request, CancellationToken cancellationToken) =>
        authenticationService.LoginAsync(new LoginInput(request.Email, request.Password), cancellationToken);
}
