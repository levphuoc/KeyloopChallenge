using Keyloop.Application.DTOs.Scheduler;
using Keyloop.Application.Features.ReferenceData;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Keyloop.API.Controllers.V1;

public sealed class CustomersController(ISender sender) : ApiControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<CustomerDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken) =>
        Ok(await sender.Send(new ListCustomersQuery(), cancellationToken));

    [HttpGet("{id:int}/vehicles")]
    [ProducesResponseType(typeof(IReadOnlyList<VehicleDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetVehicles(int id, CancellationToken cancellationToken) =>
        Ok(await sender.Send(new ListVehiclesQuery(id), cancellationToken));
}
