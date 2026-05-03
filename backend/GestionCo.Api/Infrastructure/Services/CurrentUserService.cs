using GestionCo.Api.Application.Common.Interfaces;
using GestionCo.Api.Domain.Enums;
using System.Security.Claims;

namespace GestionCo.Api.Infrastructure.Services;

public class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUserService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public int? UserId
    {
        get
        {
            var claim = _httpContextAccessor.HttpContext?.User.FindFirst(ClaimTypes.NameIdentifier);
            return claim != null && int.TryParse(claim.Value, out var id) ? id : null;
        }
    }

    public string? Email => _httpContextAccessor.HttpContext?.User.FindFirst(ClaimTypes.Email)?.Value;

    public RoleUtilisateur? Role
    {
        get
        {
            var roleClaim = _httpContextAccessor.HttpContext?.User.FindFirst(ClaimTypes.Role)?.Value;
            return roleClaim != null && Enum.TryParse<RoleUtilisateur>(roleClaim, out var role) ? role : null;
        }
    }

    public string? IpAddress => _httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString();

    public string? UserAgent => _httpContextAccessor.HttpContext?.Request.Headers.UserAgent.ToString();

    public bool IsAuthenticated => _httpContextAccessor.HttpContext?.User.Identity?.IsAuthenticated ?? false;

    public bool IsInRole(RoleUtilisateur role) => Role == role;
}
