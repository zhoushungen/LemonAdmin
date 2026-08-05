using Lemon.Api.Authorization;
using Lemon.Application.Abstractions.Security;
using Lemon.Application.Modules.Auth;
using Lemon.Application.Modules.Impersonation;
using Lemon.Application.Common;
using Lemon.Api.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Lemon.Api.Controllers.Admin.V1;

[ApiController]
[Route("api/admin/v1/impersonation")]
public sealed class ImpersonationController : ControllerBase
{
    private readonly IImpersonationService _service;
    private readonly ICurrentUser _currentUser;

    public ImpersonationController(IImpersonationService service, ICurrentUser currentUser)
    {
        _service = service;
        _currentUser = currentUser;
    }

    [RequireSuperAdmin]
    [HttpPost("start")]
    public async Task<ApiResponse<AuthResponse>> Start(StartImpersonationRequest request, CancellationToken ct)
    {
        var result = await _service.StartAsync(
            _currentUser.UserId!.Value,
            request,
            ct);
        return ApiResponse<AuthResponse>.Success(result, HttpContext.TraceIdentifier);
    }

    [Authorize]
    [HttpPost("stop")]
    public async Task<ApiResponse<AuthResponse>> Stop(CancellationToken ct)
    {
        if (!_currentUser.IsImpersonating || !_currentUser.ActorUserId.HasValue || string.IsNullOrWhiteSpace(_currentUser.ImpersonationSessionId))
            throw new AppException(ErrorCodes.ValidationFailed, "当前不在账号切换状态");

        var result = await _service.StopAsync(
            _currentUser.ActorUserId.Value,
            _currentUser.ImpersonationSessionId,
            _currentUser.IpAddress,
            ct);
        return ApiResponse<AuthResponse>.Success(result, HttpContext.TraceIdentifier);
    }
}
