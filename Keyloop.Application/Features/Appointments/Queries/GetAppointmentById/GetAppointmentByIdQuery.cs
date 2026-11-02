using Keyloop.Application.Abstractions.Scheduler;
using Keyloop.Application.DTOs.Scheduler;
using Keyloop.Application.Messaging;
using MediatR;

namespace Keyloop.Application.Features.Appointments.Queries.GetAppointmentById;
public sealed record GetAppointmentByIdQuery(Guid Id) : IQuery<AppointmentDto?>;
public sealed class GetAppointmentByIdQueryHandler(IAppointmentScheduler scheduler) : IRequestHandler<GetAppointmentByIdQuery, AppointmentDto?>
{ public Task<AppointmentDto?> Handle(GetAppointmentByIdQuery request, CancellationToken cancellationToken) => scheduler.GetAsync(request.Id, cancellationToken); }
