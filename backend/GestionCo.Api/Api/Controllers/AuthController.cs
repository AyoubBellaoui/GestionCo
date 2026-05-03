using GestionCo.Api.Application.Auth.Commands;
using GestionCo.Api.Application.Auth.DTOs;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GestionCo.Api.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly IMediator _mediator;
    public AuthController(IMediator mediator) => _mediator = mediator;

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<ActionResult<AuthResponse>> Login([FromBody] LoginRequest req, CancellationToken ct)
    {
        var result = await _mediator.Send(new LoginCommand(req.Email, req.Password), ct);
        return Ok(result);
    }

    [HttpPost("refresh")]
    [AllowAnonymous]
    public async Task<ActionResult<AuthResponse>> Refresh([FromBody] RefreshTokenRequest req, CancellationToken ct)
    {
        var result = await _mediator.Send(new RefreshTokenCommand(req.AccessToken, req.RefreshToken), ct);
        return Ok(result);
    }

    [HttpPost("logout")]
    [Authorize]
    public async Task<IActionResult> Logout(CancellationToken ct)
    {
        await _mediator.Send(new LogoutCommand(), ct);
        return Ok(new { message = "Déconnexion réussie" });
    }

    [HttpGet("me")]
    [Authorize]
    public async Task<ActionResult<UserDto>> Me(CancellationToken ct)
    {
        var user = await _mediator.Send(new GetCurrentUserQuery(), ct);
        return Ok(user);
    }
}
