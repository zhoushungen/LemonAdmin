using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Lemon.Application.Abstractions.Security;

namespace Lemon.Api.Services;

public sealed class HttpCurrentUser : ICurrentUser
{
    private readonly IHttpContextAccessor _accessor;
    public HttpCurrentUser(IHttpContextAccessor accessor) => _accessor = accessor;

    private ClaimsPrincipal? Principal => _accessor.HttpContext?.User;
    private HttpContext? Context => _accessor.HttpContext;

    public long? UserId => ParseLong(JwtRegisteredClaimNames.Sub);
    public long? ActorUserId => ParseLong("actor_sub") ?? UserId;
    public long? DepartmentId => ParseLong("department_id");
    public long? RoleId => ParseLong("role_id");
    public string? Username => Principal?.Identity?.Name ?? Principal?.FindFirst(JwtRegisteredClaimNames.UniqueName)?.Value;
    public bool IsAuthenticated => Principal?.Identity?.IsAuthenticated == true;
    public bool IsSuperAdmin => ParseBool("is_super_admin");
    public bool IsImpersonating => ParseBool("is_impersonating");
    public string? ImpersonationSessionId => Principal?.FindFirst("imp_session")?.Value;
    public string? IpAddress => Context?.Connection.RemoteIpAddress?.ToString();

    private long? ParseLong(string type) =>
        long.TryParse(Principal?.FindFirst(type)?.Value, out var value) ? value : null;

    private bool ParseBool(string type) =>
        bool.TryParse(Principal?.FindFirst(type)?.Value, out var value) && value;
}
