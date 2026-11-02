using Keyloop.Application.DTOs.Scheduler;

namespace Keyloop.Application.Abstractions.Scheduler;

public interface IAppointmentScheduler
{
    Task<AppointmentDto> CreateAsync(CreateAppointmentInput input, CancellationToken cancellationToken);
    Task<AppointmentDto?> GetAsync(Guid id, CancellationToken cancellationToken);
    Task<IReadOnlyList<DealershipDto>> ListDealershipsAsync(CancellationToken cancellationToken);
    Task<IReadOnlyList<ServiceTypeDto>> ListServiceTypesAsync(CancellationToken cancellationToken);
}

public sealed record CreateAppointmentInput(int CustomerId, int VehicleId, int DealershipId, int ServiceTypeId, DateTimeOffset StartTime);
