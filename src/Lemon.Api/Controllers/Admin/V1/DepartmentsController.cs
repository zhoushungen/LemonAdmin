using Lemon.Api.Authorization;
using Lemon.Application.Abstractions.Security;
using Lemon.Application.Modules.Admins;
using Lemon.Application.Modules.Departments;
using Lemon.Api.Contracts;
using Microsoft.AspNetCore.Mvc;

namespace Lemon.Api.Controllers.Admin.V1;

[ApiController]
[Route("api/admin/v1/departments")]
public sealed class DepartmentsController : ControllerBase
{
    private readonly IDepartmentService _service;
    private readonly ICurrentUser _currentUser;

    public DepartmentsController(IDepartmentService service, ICurrentUser currentUser)
    {
        _service = service;
        _currentUser = currentUser;
    }

    [RequirePermission("system.department.read")]
    [HttpGet]
    public async Task<ApiResponse<IReadOnlyList<DepartmentDto>>> GetAll(CancellationToken ct) =>
        ApiResponse<IReadOnlyList<DepartmentDto>>.Success(
            await _service.GetAllAsync(ct),
            HttpContext.TraceIdentifier);

    [RequirePermission("system.department.read")]
    [HttpGet("manager-options")]
    public async Task<ApiResponse<IReadOnlyList<AdminOptionDto>>> GetManagerOptions(CancellationToken ct) =>
        ApiResponse<IReadOnlyList<AdminOptionDto>>.Success(
            await _service.GetManagerOptionsAsync(ct),
            HttpContext.TraceIdentifier);

    [RequirePermission("system.department.create")]
    [HttpPost]
    public async Task<ApiResponse<object>> Create(SaveDepartmentRequest request, CancellationToken ct) =>
        ApiResponse<object>.Success(
            new { id = await _service.CreateAsync(request, _currentUser.UserId!.Value, ct) },
            HttpContext.TraceIdentifier);

    [RequirePermission("system.department.update")]
    [HttpPut("{id:long}")]
    public async Task<ApiResponse> Update(long id, SaveDepartmentRequest request, CancellationToken ct)
    {
        await _service.UpdateAsync(id, request, _currentUser.UserId!.Value, ct);
        return ApiResponse.Success(traceId: HttpContext.TraceIdentifier);
    }

    [RequirePermission("system.department.delete")]
    [HttpDelete("{id:long}")]
    public async Task<ApiResponse> Delete(long id, CancellationToken ct)
    {
        await _service.DeleteAsync(id, _currentUser.UserId!.Value, ct);
        return ApiResponse.Success(traceId: HttpContext.TraceIdentifier);
    }
}
