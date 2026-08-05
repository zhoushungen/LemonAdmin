using Lemon.Application.Modules.Settings;
using Lemon.Api.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Lemon.Api.Controllers.Public.V1;

[ApiController]
[AllowAnonymous]
[Route("api/public/v1/settings")]
public sealed class PublicSettingsController : ControllerBase
{
    private readonly ISettingService _service;
    public PublicSettingsController(ISettingService service) => _service = service;

    [HttpGet]
    public async Task<ApiResponse<IReadOnlyList<SystemSettingDto>>> Get(CancellationToken cancellationToken)
    {
        var data = (await _service.GetAllAsync(cancellationToken)).Where(x => x.IsPublic && !x.IsEncrypted).ToArray();
        return ApiResponse<IReadOnlyList<SystemSettingDto>>.Success(data, HttpContext.TraceIdentifier);
    }
}
