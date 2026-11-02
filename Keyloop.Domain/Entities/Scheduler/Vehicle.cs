namespace Keyloop.Domain.Entities.Scheduler;

public sealed class Vehicle : Entity<int>
{
    public int CustomerId { get; private set; }
    public string Vin { get; private set; } = null!;
    public string Make { get; private set; } = null!;
    public string Model { get; private set; } = null!;
    private Vehicle() { }
    public Vehicle(int id, int customerId, string vin, string make, string model)
    { Id = id; CustomerId = customerId; Vin = vin; Make = make; Model = model; }
}
