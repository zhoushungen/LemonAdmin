using Lemon.Api.Authorization;
using Lemon.Application.Abstractions.Security;
using Lemon.Application.Modules.Settings;
using Lemon.Api.Contracts;
using Microsoft.AspNetCore.Mvc;

namespace Lemon.Api.Controllers.Admin.V1;

[ApiController]
[Route("api/admin/v1/settings")]
public sealed class SettingsController : ControllerBase
{
    private readonly ISettingService _service;
    private readonly ICurrentUser _currentUser;

    public SettingsController(ISettingService service, ICurrentUser currentUser)
    {
        _service = service;
        _currentUser = currentUser;
    }

    [RequirePermission("system.setting.read")]
    [HttpGet]
    public async Task<ApiResponse<IReadOnlyList<SystemSettingDto>>> GetAll(CancellationToken ct) =>
        ApiResponse<IReadOnlyList<SystemSettingDto>>.Success(await _service.GetAllAsync(ct), HttpContext.TraceIdentifier);

    [RequireSuperAdmin]
    [HttpPut("{key}")]
    public async Task<ApiResponse> Upsert(string key, UpsertSettingRequest request, CancellationToken ct)
    {
        await _service.UpsertAsync(key, request, _currentUser.UserId!.Value, ct);
        return ApiResponse.Success(traceId: HttpContext.TraceIdentifier);
    }
}
