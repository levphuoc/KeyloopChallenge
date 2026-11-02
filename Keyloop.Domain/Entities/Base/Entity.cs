namespace Keyloop.Domain.Entities;

public abstract class Entity<TKey> : IEntity<TKey>
{
    public TKey Id { get; protected set; } = default!;
}

public abstract class Entity : Entity<Guid>
{
    protected Entity() => Id = Guid.NewGuid();
}
