namespace Keyloop.Domain.Exceptions;

public sealed class NotFoundException : DomainException
{
    public NotFoundException(string name, object key)
        : base($"{name} '{key}' was not found.", "not_found")
    {
    }

    public NotFoundException(string message)
        : base(message, "not_found")
    {
    }
}
