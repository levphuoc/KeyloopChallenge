using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Keyloop.Application.Abstractions.Authentication;
using Keyloop.Application.Abstractions.Persistence;
using Keyloop.Domain.Entities.Identity;
using Keyloop.Domain.Exceptions;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace Keyloop.Infrastructure.Authentication;

internal sealed class AuthenticationService(
    IRepository<ApplicationUser, Guid> users,
    IOptions<JwtOptions> jwtOptions,
    IOptions<AdminAccountOptions> adminOptions,
    TimeProvider timeProvider) : IAuthenticationService
{
    public async Task<AuthToken> RegisterAsync(RegisterUserInput input, CancellationToken cancellationToken = default)
    {
        var normalizedEmail = NormalizeEmail(input.Email);
        if (await users.ListAsync(user => user.NormalizedEmail == normalizedEmail, cancellationToken) is { Count: > 0 })
            throw new ConflictException("An account with this email already exists.");

        var user = new ApplicationUser(input.Email, input.DisplayName, PasswordHashing.Hash(input.Password));
        await users.AddAsync(user, cancellationToken);
        return CreateToken(user.Id.ToString(), user.Email, user.Role);
    }

    public async Task<AuthToken> LoginAsync(LoginInput input, CancellationToken cancellationToken = default)
    {
        var admin = adminOptions.Value;
        if (string.Equals(input.Email, admin.Email, StringComparison.OrdinalIgnoreCase)
            && PasswordHashing.FixedTimeEquals(input.Password, admin.Password))
            return CreateToken("global-admin", admin.Email, "Administrator");

        var normalizedEmail = NormalizeEmail(input.Email);
        var user = (await users.ListAsync(candidate => candidate.NormalizedEmail == normalizedEmail, cancellationToken)).SingleOrDefault();
        if (user is null || !PasswordHashing.Verify(input.Password, user.PasswordHash))
            throw new UnauthorizedAccessException("Invalid email or password.");

        return CreateToken(user.Id.ToString(), user.Email, user.Role);
    }

    private AuthToken CreateToken(string subject, string email, string role)
    {
        var options = jwtOptions.Value;
        var now = timeProvider.GetUtcNow();
        var expiresAt = now.AddMinutes(options.ExpiryMinutes);
        var credentials = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(options.SigningKey)),
            SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: options.Issuer,
            audience: options.Audience,
            claims:
            [
                new(JwtRegisteredClaimNames.Sub, subject),
                new(JwtRegisteredClaimNames.Email, email),
                new(ClaimTypes.Role, role),
                new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            ],
            notBefore: now.UtcDateTime,
            expires: expiresAt.UtcDateTime,
            signingCredentials: credentials);

        return new AuthToken(new JwtSecurityTokenHandler().WriteToken(token), expiresAt, email, role);
    }

    private static string NormalizeEmail(string email) => email.Trim().ToUpperInvariant();
}

internal static class PasswordHashing
{
    private const int Iterations = 210_000;
    private const int SaltSize = 16;
    private const int HashSize = 32;

    public static string Hash(string password)
    {
        var salt = RandomNumberGenerator.GetBytes(SaltSize);
        var hash = Rfc2898DeriveBytes.Pbkdf2(password, salt, Iterations, HashAlgorithmName.SHA512, HashSize);
        return $"v1.{Iterations}.{Convert.ToBase64String(salt)}.{Convert.ToBase64String(hash)}";
    }

    public static bool Verify(string password, string encodedHash)
    {
        var parts = encodedHash.Split('.');
        if (parts.Length != 4 || parts[0] != "v1" || !int.TryParse(parts[1], out var iterations))
            return false;

        try
        {
            var salt = Convert.FromBase64String(parts[2]);
            var expectedHash = Convert.FromBase64String(parts[3]);
            var actualHash = Rfc2898DeriveBytes.Pbkdf2(password, salt, iterations, HashAlgorithmName.SHA512, expectedHash.Length);
            return CryptographicOperations.FixedTimeEquals(actualHash, expectedHash);
        }
        catch (FormatException)
        {
            return false;
        }
    }

    public static bool FixedTimeEquals(string left, string right) =>
        CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(left), Encoding.UTF8.GetBytes(right));
}
