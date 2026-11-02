using FluentValidation;
using Keyloop.Application.Abstractions.Scheduler;
using Keyloop.Application.DTOs.Scheduler;
using Keyloop.Application.Messaging;
using MediatR;

namespace Keyloop.Application.Features.Appointments.Commands.CreateAppointment;

public sealed record CreateAppointmentCommand(int CustomerId, int VehicleId, int DealershipId, int ServiceTypeId, DateTimeOffset StartTime) : ICommand<AppointmentDto>;

public sealed class CreateAppointmentCommandValidator : AbstractValidator<CreateAppointmentCommand>
{
    public CreateAppointmentCommandValidator()
    {
        RuleFor(x => x.CustomerId).GreaterThan(0); RuleFor(x => x.VehicleId).GreaterThan(0);
        RuleFor(x => x.DealershipId).GreaterThan(0); RuleFor(x => x.ServiceTypeId).GreaterThan(0);
        RuleFor(x => x.StartTime).NotEqual(default(DateTimeOffset));
    }
}

public sealed class CreateAppointmentCommandHandler(IAppointmentScheduler scheduler) : IRequestHandler<CreateAppointmentCommand, AppointmentDto>
{
    public Task<AppointmentDto> Handle(CreateAppointmentCommand request, CancellationToken cancellationToken) =>
        scheduler.CreateAsync(new(request.CustomerId, request.VehicleId, request.DealershipId, request.ServiceTypeId, request.StartTime), cancellationToken);
}
