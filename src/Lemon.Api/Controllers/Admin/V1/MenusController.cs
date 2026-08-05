using Lemon.Api.Authorization;
using Lemon.Api.Contracts;
using Lemon.Application.Abstractions.Security;
using Lemon.Application.Modules.Menus;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Lemon.Api.Controllers.Admin.V1;

[ApiController]
[Route("api/admin/v1/menus")]
public sealed class MenusController : ControllerBase
{
    private readonly IMenuService _service;
    private readonly ICurrentUser _currentUser;

    public MenusController(IMenuService service, ICurrentUser currentUser)
    {
        _service = service;
        _currentUser = currentUser;
    }

    [RequirePermission("system.menu.read")]
    [HttpGet]
    public async Task<ApiResponse<IReadOnlyList<MenuDto>>> GetAll(CancellationToken ct) =>
        ApiResponse<IReadOnlyList<MenuDto>>.Success(
            await _service.GetAllAsync(ct),
            HttpContext.TraceIdentifier);

    [Authorize]
    [HttpGet("current")]
    public async Task<ApiResponse<IReadOnlyList<MenuDto>>> GetCurrent(CancellationToken ct) =>
        ApiResponse<IReadOnlyList<MenuDto>>.Success(
            await _service.GetCurrentAsync(_currentUser.UserId!.Value, ct),
            HttpContext.TraceIdentifier);

    [RequirePermission("system.menu.create")]
    [HttpPost]
    public async Task<ApiResponse<object>> Create(SaveMenuRequest request, CancellationToken ct) =>
        ApiResponse<object>.Success(
            new { id = await _service.CreateAsync(request, _currentUser.UserId!.Value, ct) },
            HttpContext.TraceIdentifier);

    [RequirePermission("system.menu.update")]
    [HttpPut("{id:long}")]
    public async Task<ApiResponse> Update(long id, SaveMenuRequest request, CancellationToken ct)
    {
        await _service.UpdateAsync(id, request, _currentUser.UserId!.Value, ct);
        return ApiResponse.Success(traceId: HttpContext.TraceIdentifier);
    }

    [RequirePermission("system.menu.delete")]
    [HttpDelete("{id:long}")]
    public async Task<ApiResponse> Delete(long id, CancellationToken ct)
    {
        await _service.DeleteAsync(id, _currentUser.UserId!.Value, ct);
        return ApiResponse.Success(traceId: HttpContext.TraceIdentifier);
    }
}
