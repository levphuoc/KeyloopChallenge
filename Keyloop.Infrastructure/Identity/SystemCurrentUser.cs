using Keyloop.Application.Abstractions;

namespace Keyloop.Infrastructure.Identity;

internal sealed class SystemCurrentUser : ICurrentUser
{
    public string? UserId => "system";
    public bool IsAuthenticated => false;
}
