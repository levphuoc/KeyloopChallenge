namespace Keyloop.Domain.Exceptions;

public sealed class SlotUnavailableException(string code, string message) : DomainException(message, code);
