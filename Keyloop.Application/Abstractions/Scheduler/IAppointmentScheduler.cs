using Keyloop.Application.DTOs.Scheduler;

namespace Keyloop.Application.Abstractions.Scheduler;

public interface IAppointmentScheduler
{
    Task<AppointmentDto> CreateAsync(CreateAppointmentInput input, CancellationToken cancellationToken);

    Task<AppointmentDto?> GetAsync(Guid id, CancellationToken cancellationToken);

    Task<AppointmentDto> CancelAsync(Guid id, CancellationToken cancellationToken);

    Task<IReadOnlyList<AvailableSlotDto>> GetAvailabilityAsync(
        int dealershipId, int serviceTypeId, DateOnly date, CancellationToken cancellationToken);

    Task<IReadOnlyList<DealershipDto>> ListDealershipsAsync(CancellationToken cancellationToken);

    Task<IReadOnlyList<ServiceTypeDto>> ListServiceTypesAsync(CancellationToken cancellationToken);

    Task<IReadOnlyList<CustomerDto>> ListCustomersAsync(CancellationToken cancellationToken);

    Task<IReadOnlyList<VehicleDto>> ListVehiclesAsync(int customerId, CancellationToken cancellationToken);
}

public sealed record CreateAppointmentInput(
    int CustomerId, int VehicleId, int DealershipId, int ServiceTypeId, DateTimeOffset StartTime);
