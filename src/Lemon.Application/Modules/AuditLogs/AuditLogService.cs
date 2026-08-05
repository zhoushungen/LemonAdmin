using Lemon.Application.Abstractions.Repositories;
using Lemon.Application.Abstractions.Security;
using Lemon.Application.Common;
using Lemon.Domain.System;

namespace Lemon.Application.Modules.AuditLogs;

public sealed class AuditLogService : IAuditLogService
{
    private readonly IAuditLogRepository _repository;
    private readonly IDataScopeService _dataScope;

    public AuditLogService(IAuditLogRepository repository, IDataScopeService dataScope)
    {
        _repository = repository;
        _dataScope = dataScope;
    }

    public async Task<PagedResult<AuditLogDto>> GetPagedAsync(
        int pageIndex,
        int pageSize,
        string? keyword,
        string? module,
        CancellationToken ct = default)
    {
        var scope = await _dataScope.GetCurrentAsync(ct);
        var result = await _repository.GetPagedAsync(pageIndex, pageSize, keyword, module, scope, ct);
        return new PagedResult<AuditLogDto>(
            result.Items.Select(ToDto).ToArray(),
            result.PageIndex,
            result.PageSize,
            result.Total);
    }

    private static AuditLogDto ToDto(AuditLog log) => new(
        log.Id,
        log.AdminUserId,
        log.DepartmentId,
        log.ActorAdminUserId,
        log.IsImpersonating,
        log.Module,
        log.Action,
        log.RequestPath,
        log.HttpMethod,
        log.StatusCode,
        log.IpAddress,
        log.ElapsedMilliseconds,
        log.TraceId,
        log.CreatedAt);
}
