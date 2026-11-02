using Keyloop.Application.DTOs.Scheduler;
using Keyloop.Application.Features.ReferenceData;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Keyloop.API.Controllers.V1;

public sealed class ServiceTypesController(ISender sender) : ApiControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<ServiceTypeDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken) =>
        Ok(await sender.Send(new ListServiceTypesQuery(), cancellationToken));
}
