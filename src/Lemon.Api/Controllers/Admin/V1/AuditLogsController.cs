using Lemon.Api.Authorization;
using Lemon.Api.Contracts;
using Lemon.Application.Common;
using Lemon.Application.Modules.AuditLogs;
using Microsoft.AspNetCore.Mvc;

namespace Lemon.Api.Controllers.Admin.V1;

[ApiController]
[Route("api/admin/v1/audit-logs")]
public sealed class AuditLogsController : ControllerBase
{
    private readonly IAuditLogService _service;

    public AuditLogsController(IAuditLogService service) => _service = service;

    [RequirePermission("system.audit.read")]
    [HttpGet]
    public async Task<ApiResponse<PagedResult<AuditLogDto>>> GetPaged(
        [FromQuery] int pageIndex = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? keyword = null,
        [FromQuery] string? module = null,
        CancellationToken ct = default) =>
        ApiResponse<PagedResult<AuditLogDto>>.Success(
            await _service.GetPagedAsync(pageIndex, pageSize, keyword, module, ct),
            HttpContext.TraceIdentifier);
}
