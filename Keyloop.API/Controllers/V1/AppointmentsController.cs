using Keyloop.API.DTOs.Appointments.Requests;
using Keyloop.Application.DTOs.Scheduler;
using Keyloop.Application.Features.Appointments.Commands.CancelAppointment;
using Keyloop.Application.Features.Appointments.Commands.CreateAppointment;
using Keyloop.Application.Features.Appointments.Queries.GetAppointmentById;
using Keyloop.Domain.Exceptions;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Keyloop.API.Controllers.V1;

public sealed class AppointmentsController(ISender sender) : ApiControllerBase
{
    [HttpPost]
    [ProducesResponseType(typeof(AppointmentDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Create(
        CreateAppointmentRequest request, CancellationToken cancellationToken)
    {
        var appointment = await sender.Send(
            new CreateAppointmentCommand(
                request.CustomerId,
                request.VehicleId,
                request.DealershipId,
                request.ServiceTypeId,
                request.StartTime),
            cancellationToken);

        return CreatedAtAction(
            nameof(GetById), new { id = appointment.Id, version = "1" }, appointment);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(AppointmentDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var appointment = await sender.Send(new GetAppointmentByIdQuery(id), cancellationToken);
        if (appointment is null)
            throw new NotFoundException("Appointment", id);
        return Ok(appointment);
    }

    [HttpPost("{id:guid}/cancel")]
    [ProducesResponseType(typeof(AppointmentDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Cancel(Guid id, CancellationToken cancellationToken)
    {
        var appointment = await sender.Send(new CancelAppointmentCommand(id), cancellationToken);
        return Ok(appointment);
    }
}
