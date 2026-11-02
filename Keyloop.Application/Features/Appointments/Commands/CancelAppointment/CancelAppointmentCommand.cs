using FluentValidation;
using Keyloop.Application.Abstractions.Scheduler;
using Keyloop.Application.DTOs.Scheduler;
using Keyloop.Application.Messaging;
using MediatR;

namespace Keyloop.Application.Features.Appointments.Commands.CancelAppointment;

public sealed record CancelAppointmentCommand(Guid Id) : ICommand<AppointmentDto>;

public sealed class CancelAppointmentCommandValidator : AbstractValidator<CancelAppointmentCommand>
{
    public CancelAppointmentCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
    }
}

public sealed class CancelAppointmentCommandHandler(IAppointmentScheduler scheduler)
    : IRequestHandler<CancelAppointmentCommand, AppointmentDto>
{
    public Task<AppointmentDto> Handle(CancelAppointmentCommand request, CancellationToken cancellationToken) =>
        scheduler.CancelAsync(request.Id, cancellationToken);
}
