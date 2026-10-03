using GestionCo.Api.Application.Auth.Commands;
using GestionCo.Api.Application.Auth.DTOs;
using GestionCo.Api.Application.Common.Exceptions;
using GestionCo.Api.Application.Common.Interfaces;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GestionCo.Api.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILoginAttemptLimiter _attempts;

    public AuthController(IMediator mediator, ILoginAttemptLimiter attempts)
    {
        _mediator = mediator; _attempts = attempts;
    }

    // RG-S2 : seuls les mots de passe erronés sont comptés (5 par IP sur 15 minutes) ;
    // une connexion réussie n'est jamais bloquée et remet le compteur à zéro
    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<ActionResult<AuthResponse>> Login([FromBody] LoginRequest req, CancellationToken ct)
    {
        var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "inconnue";
        if (_attempts.IsBlocked(ip, out var retryAfter))
        {
            Response.Headers["Retry-After"] = ((int)Math.Ceiling(retryAfter.TotalSeconds)).ToString();
            return StatusCode(StatusCodes.Status429TooManyRequests,
                new { message = "Trop de tentatives de connexion. Réessayez dans 15 minutes." });
        }

        try
        {
            var result = await _mediator.Send(new LoginCommand(req.Email, req.Password), ct);
            _attempts.Reset(ip);
            return Ok(result);
        }
        catch (UnauthorizedException)
        {
            _attempts.RecordFailure(ip);
            throw;
        }
    }

    // Pas de limitation : le renouvellement exige un refresh token valide (rotation)
    [HttpPost("refresh")]
    [AllowAnonymous]
    public async Task<ActionResult<AuthResponse>> Refresh([FromBody] RefreshTokenRequest req, CancellationToken ct)
    {
        var result = await _mediator.Send(new RefreshTokenCommand(req.AccessToken, req.RefreshToken), ct);
        return Ok(result);
    }

    [HttpPost("logout")]
    [AllowAnonymous]
    public async Task<IActionResult> Logout([FromHeader(Name = "Authorization")] string? authHeader, CancellationToken ct)
    {
        var token = authHeader?.StartsWith("Bearer ") == true ? authHeader["Bearer ".Length..] : null;
        await _mediator.Send(new LogoutCommand(token), ct);
        return Ok(new { message = "Déconnexion réussie" });
    }

    [HttpGet("me")]
    [Authorize]
    public async Task<ActionResult<UserDto>> Me(CancellationToken ct)
    {
        var user = await _mediator.Send(new GetCurrentUserQuery(), ct);
        return Ok(user);
    }

    [HttpPut("profile")]
    [Authorize]
    public async Task<ActionResult<UserDto>> UpdateProfile([FromBody] UpdateProfileRequest req, CancellationToken ct)
    {
        var user = await _mediator.Send(new UpdateProfileCommand(req.Prenom, req.Nom, req.Telephone), ct);
        return Ok(user);
    }

    [HttpPost("change-password")]
    [Authorize]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest req, CancellationToken ct)
    {
        await _mediator.Send(new ChangePasswordCommand(req.CurrentPassword, req.NewPassword), ct);
        return Ok(new { message = "Mot de passe modifié avec succès" });
    }
}

public class UpdateProfileRequest
{
    public string Prenom { get; set; } = string.Empty;
    public string Nom { get; set; } = string.Empty;
    public string? Telephone { get; set; }
}

public class ChangePasswordRequest
{
    public string CurrentPassword { get; set; } = string.Empty;
    public string NewPassword { get; set; } = string.Empty;
}
