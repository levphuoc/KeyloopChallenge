namespace Keyloop.Infrastructure.Authentication;

public sealed class JwtOptions
{
    public const string SectionName = "Authentication:Jwt";
    public string Issuer { get; init; } = null!;
    public string Audience { get; init; } = null!;
    public string SigningKey { get; init; } = null!;
    public int ExpiryMinutes { get; init; } = 60;
}

public sealed class AdminAccountOptions
{
    public const string SectionName = "Authentication:Admin";
    public string Email { get; init; } = null!;
    public string Password { get; init; } = null!;
    public string DisplayName { get; init; } = "Global Administrator";
}
