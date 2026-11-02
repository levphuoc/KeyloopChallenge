namespace Keyloop.Domain.Entities.Identity;

public sealed class ApplicationUser : AuditableEntity
{
    public string Email { get; private set; } = null!;
    public string NormalizedEmail { get; private set; } = null!;
    public string DisplayName { get; private set; } = null!;
    public string PasswordHash { get; private set; } = null!;
    public string Role { get; private set; } = "User";

    private ApplicationUser()
    {
    }

    public ApplicationUser(string email, string displayName, string passwordHash)
    {
        Email = email.Trim();
        NormalizedEmail = Email.ToUpperInvariant();
        DisplayName = displayName.Trim();
        PasswordHash = passwordHash;
    }
}
