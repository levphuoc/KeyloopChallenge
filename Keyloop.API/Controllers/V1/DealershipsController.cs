using Keyloop.Application.DTOs.Scheduler;
using Keyloop.Application.Features.Appointments.Queries.GetAvailability;
using Keyloop.Application.Features.ReferenceData;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Keyloop.API.Controllers.V1;

public sealed class DealershipsController(ISender sender) : ApiControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<DealershipDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken) =>
        Ok(await sender.Send(new ListDealershipsQuery(), cancellationToken));

    [HttpGet("{id:int}/availability")]
    [ProducesResponseType(typeof(IReadOnlyList<AvailableSlotDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetAvailability(
        int id,
        [FromQuery] int serviceTypeId,
        [FromQuery] DateOnly? date,
        CancellationToken cancellationToken) =>
        Ok(await sender.Send(
            new GetAvailabilityQuery(
                id,
                serviceTypeId,
                date ?? DateOnly.FromDateTime(DateTime.Today).AddDays(1)),
            cancellationToken));
}
