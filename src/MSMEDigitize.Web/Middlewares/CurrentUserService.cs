using System.Security.Claims;
using MSMEDigitize.Core.Enums;
using MSMEDigitize.Core.Interfaces;

namespace MSMEDigitize.Web.Middlewares;

public class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUserService(IHttpContextAccessor httpContextAccessor)
        => _httpContextAccessor = httpContextAccessor;

    private ClaimsPrincipal? User => _httpContextAccessor.HttpContext?.User;

    public Guid? TenantId
    {
        get
        {
            var id = User?.FindFirstValue("TenantId");
            return id != null ? Guid.Parse(id) : null;
        }
    }

    public Guid UserId
    {
        get
        {
            var id = User?.FindFirstValue(ClaimTypes.NameIdentifier);
            return id != null ? Guid.Parse(id) : Guid.Empty;
        }
    }

    public string Email    => User?.FindFirstValue(ClaimTypes.Email)        ?? string.Empty;
    public string FullName => User?.FindFirstValue("FullName")              ?? string.Empty;
    public string BusinessName => User?.FindFirstValue("BusinessName")      ?? string.Empty;

    public TenantRole Role
    {
        get
        {
            var role = User?.FindFirstValue(ClaimTypes.Role);
            return Enum.TryParse<TenantRole>(role, out var r) ? r : TenantRole.ReadOnly;
        }
    }

    public bool   IsAuthenticated => User?.Identity?.IsAuthenticated ?? false;
    public string? IpAddress      => _httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString();
    public bool    IsSuperAdmin   => User?.IsInRole("SuperAdmin") ?? false;
}
