namespace Keyloop.Domain.Entities;

public interface IHasConcurrencyToken
{
    byte[] RowVersion { get; set; }
}
