using Lemon.Api.Authorization;
using Lemon.Api.Contracts;
using Lemon.Application.Abstractions.Security;
using Lemon.Application.Modules.Roles;
using Microsoft.AspNetCore.Mvc;

namespace Lemon.Api.Controllers.Admin.V1;

[ApiController]
[Route("api/admin/v1/roles")]
public sealed class RolesController : ControllerBase
{
    private readonly IRoleService _service;
    private readonly ICurrentUser _currentUser;

    public RolesController(IRoleService service, ICurrentUser currentUser)
    {
        _service = service;
        _currentUser = currentUser;
    }

    [RequirePermission("system.role.read")]
    [HttpGet]
    public async Task<ApiResponse<IReadOnlyList<RoleDto>>> GetAll(CancellationToken ct) =>
        ApiResponse<IReadOnlyList<RoleDto>>.Success(
            await _service.GetAllAsync(ct),
            HttpContext.TraceIdentifier);

    [RequirePermission("system.role.read")]
    [HttpGet("permissions")]
    public async Task<ApiResponse<IReadOnlyList<PermissionDto>>> GetPermissions(CancellationToken ct) =>
        ApiResponse<IReadOnlyList<PermissionDto>>.Success(
            await _service.GetPermissionsAsync(ct),
            HttpContext.TraceIdentifier);

    [RequirePermission("system.role.create")]
    [HttpPost]
    public async Task<ApiResponse<object>> Create(CreateRoleRequest request, CancellationToken ct) =>
        ApiResponse<object>.Success(
            new { id = await _service.CreateAsync(request, _currentUser.UserId!.Value, ct) },
            HttpContext.TraceIdentifier);

    [RequirePermission("system.role.update")]
    [HttpPut("{id:long}")]
    public async Task<ApiResponse> Update(long id, UpdateRoleRequest request, CancellationToken ct)
    {
        await _service.UpdateAsync(id, request, _currentUser.UserId!.Value, ct);
        return ApiResponse.Success(traceId: HttpContext.TraceIdentifier);
    }

    [RequirePermission("system.role.update")]
    [HttpPut("{id:long}/permissions")]
    public async Task<ApiResponse> UpdatePermissions(
        long id,
        UpdateRolePermissionsRequest request,
        CancellationToken ct)
    {
        await _service.UpdatePermissionsAsync(id, request.PermissionIds, _currentUser.UserId!.Value, ct);
        return ApiResponse.Success(traceId: HttpContext.TraceIdentifier);
    }
}
