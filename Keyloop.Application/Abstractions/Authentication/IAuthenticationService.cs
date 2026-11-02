namespace Keyloop.Application.Abstractions.Authentication;

public interface IAuthenticationService
{
    Task<AuthToken> RegisterAsync(RegisterUserInput input, CancellationToken cancellationToken = default);
    Task<AuthToken> LoginAsync(LoginInput input, CancellationToken cancellationToken = default);
}

public sealed record RegisterUserInput(string Email, string Password, string DisplayName);
public sealed record LoginInput(string Email, string Password);
public sealed record AuthToken(string AccessToken, DateTimeOffset ExpiresAt, string Email, string Role);
