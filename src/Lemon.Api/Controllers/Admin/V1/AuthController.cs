using Lemon.Application.Abstractions.Security;
using Lemon.Application.Modules.Auth;
using Lemon.Application.Modules.Impersonation;
using Lemon.Api.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Lemon.Api.Controllers.Admin.V1;

[ApiController]
[Route("api/admin/v1/auth")]
public sealed class AuthController : ControllerBase
{
    private readonly IAuthService _auth;
    private readonly IImpersonationService _impersonation;
    private readonly ICurrentUser _currentUser;

    public AuthController(IAuthService auth, IImpersonationService impersonation, ICurrentUser currentUser)
    {
        _auth = auth;
        _impersonation = impersonation;
        _currentUser = currentUser;
    }

    [AllowAnonymous]
    [HttpPost("login")]
    public async Task<ApiResponse<AuthResponse>> Login(AdminLoginRequest request, CancellationToken ct) =>
        ApiResponse<AuthResponse>.Success(await _auth.LoginAsync(request, _currentUser.IpAddress, ct), HttpContext.TraceIdentifier);

    [AllowAnonymous]
    [HttpPost("refresh")]
    public async Task<ApiResponse<AuthResponse>> Refresh(RefreshTokenRequest request, CancellationToken ct) =>
        ApiResponse<AuthResponse>.Success(await _auth.RefreshAsync(request, _currentUser.IpAddress, ct), HttpContext.TraceIdentifier);

    [Authorize]
    [HttpPost("logout-all")]
    public async Task<ApiResponse> LogoutAll(CancellationToken ct)
    {
        var actorUserId = _currentUser.ActorUserId ?? _currentUser.UserId;
        if (actorUserId is null)
            return ApiResponse.Fail("AUTH_1001", "登录身份无效", HttpContext.TraceIdentifier);

        await _impersonation.CancelSessionAsync(_currentUser.ImpersonationSessionId, ct);
        await _auth.RevokeAllAsync(actorUserId.Value, ct);
        return ApiResponse.Success(traceId: HttpContext.TraceIdentifier);
    }
}
