namespace Keyloop.Domain.Exceptions;

public sealed class ForbiddenException : DomainException
{
    public ForbiddenException(string message = "You are not allowed to perform this action.")
        : base(message, "forbidden")
    {
    }
}
