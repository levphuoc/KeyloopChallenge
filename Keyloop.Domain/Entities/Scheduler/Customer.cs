namespace Keyloop.Domain.Entities.Scheduler;

public sealed class Customer : Entity<int>
{
    public string FullName { get; private set; } = null!;
    private Customer() { }
    public Customer(int id, string fullName) { Id = id; FullName = fullName; }
}
