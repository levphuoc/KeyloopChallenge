using FluentValidation;
using Keyloop.Application.Abstractions.Scheduler;
using Keyloop.Application.DTOs.Scheduler;
using Keyloop.Application.Messaging;
using MediatR;

namespace Keyloop.Application.Features.Appointments.Queries.GetAvailability;

public sealed record GetAvailabilityQuery(int DealershipId, int ServiceTypeId, DateOnly Date)
    : IQuery<IReadOnlyList<AvailableSlotDto>>;

public sealed class GetAvailabilityQueryValidator : AbstractValidator<GetAvailabilityQuery>
{
    public GetAvailabilityQueryValidator()
    {
        RuleFor(x => x.DealershipId).GreaterThan(0);
        RuleFor(x => x.ServiceTypeId).GreaterThan(0);
        RuleFor(x => x.Date).NotEqual(default(DateOnly));
    }
}

public sealed class GetAvailabilityQueryHandler(IAppointmentScheduler scheduler)
    : IRequestHandler<GetAvailabilityQuery, IReadOnlyList<AvailableSlotDto>>
{
    public Task<IReadOnlyList<AvailableSlotDto>> Handle(
        GetAvailabilityQuery request, CancellationToken cancellationToken) =>
        scheduler.GetAvailabilityAsync(
            request.DealershipId, request.ServiceTypeId, request.Date, cancellationToken);
}
