using Keyloop.Application.Features.Authentication;
using Keyloop.API.DTOs.Authentication.Requests;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Keyloop.API.Controllers.V1;

public sealed class AuthenticationController(ISender sender) : ApiControllerBase
{
    [AllowAnonymous]
    [HttpPost("register")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    public async Task<IActionResult> Register(RegisterRequest request, CancellationToken cancellationToken)
    {
        var token = await sender.Send(new RegisterUserCommand(request.Email, request.Password, request.DisplayName), cancellationToken);
        return Ok(token);
    }

    [AllowAnonymous]
    [HttpPost("login")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    public async Task<IActionResult> Login(LoginRequest request, CancellationToken cancellationToken)
    {
        var token = await sender.Send(new LoginQuery(request.Email, request.Password), cancellationToken);
        return Ok(token);
    }

    [Authorize]
    [HttpGet("me")]
    public IActionResult Me() => Ok(new
    {
        User.Identity?.Name,
        Email = User.FindFirst(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Email)?.Value,
        Roles = User.FindAll(System.Security.Claims.ClaimTypes.Role).Select(claim => claim.Value)
    });
}

