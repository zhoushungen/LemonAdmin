using Lemon.Api.Authorization;
using Lemon.Application.Abstractions.Security;
using Lemon.Application.Modules.Admins;
using Lemon.Application.Common;
using Lemon.Api.Contracts;
using Microsoft.AspNetCore.Mvc;

namespace Lemon.Api.Controllers.Admin.V1;

[ApiController]
[Route("api/admin/v1/admins")]
public sealed class AdminsController : ControllerBase
{
    private readonly IAdminService _service;
    private readonly ICurrentUser _currentUser;

    public AdminsController(IAdminService service, ICurrentUser currentUser)
    {
        _service = service;
        _currentUser = currentUser;
    }

    [RequirePermission("system.admin.read")]
    [HttpGet]
    public async Task<ApiResponse<PagedResult<AdminDto>>> GetPaged(
        [FromQuery] int pageIndex = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? keyword = null,
        [FromQuery] long? departmentId = null,
        [FromQuery] bool? enabled = null,
        CancellationToken ct = default) =>
        ApiResponse<PagedResult<AdminDto>>.Success(
            await _service.GetPagedAsync(pageIndex, pageSize, keyword, departmentId, enabled, ct),
            HttpContext.TraceIdentifier);

    [RequirePermission("system.admin.read")]
    [HttpGet("{id:long}")]
    public async Task<ApiResponse<AdminDetailDto>> Get(long id, CancellationToken ct) =>
        ApiResponse<AdminDetailDto>.Success(await _service.GetAsync(id, ct), HttpContext.TraceIdentifier);

    [RequireSuperAdmin]
    [HttpPost]
    public async Task<ApiResponse<object>> Create(CreateAdminRequest request, CancellationToken ct) =>
        ApiResponse<object>.Success(
            new { id = await _service.CreateAsync(request, _currentUser.UserId!.Value, ct) },
            HttpContext.TraceIdentifier);

    [RequireSuperAdmin]
    [HttpPut("{id:long}")]
    public async Task<ApiResponse> Update(long id, UpdateAdminRequest request, CancellationToken ct)
    {
        await _service.UpdateAsync(id, request, _currentUser.UserId!.Value, ct);
        return ApiResponse.Success(traceId: HttpContext.TraceIdentifier);
    }

    [RequireSuperAdmin]
    [HttpPut("{id:long}/status")]
    public async Task<ApiResponse> Status(long id, ChangeAdminStatusRequest request, CancellationToken ct)
    {
        await _service.ChangeStatusAsync(id, request.Enabled, _currentUser.UserId!.Value, ct);
        return ApiResponse.Success(traceId: HttpContext.TraceIdentifier);
    }
}
