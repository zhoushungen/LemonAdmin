using Dapper;
using Lemon.Application.Abstractions.Database;
using Lemon.Application.Abstractions.Repositories;
using Lemon.Application.Abstractions.Security;
using Lemon.Domain.System;
using Lemon.Application.Common;

namespace Lemon.Infrastructure.Repositories;

public sealed class AuditLogRepository : IAuditLogRepository
{
    private readonly IDbConnectionFactory _factory;
    public AuditLogRepository(IDbConnectionFactory factory) => _factory = factory;

    public async Task WriteAsync(AuditLog log, CancellationToken ct = default)
    {
        const string sql = """
            INSERT INTO sys_audit_log(
                admin_user_id,department_id,actor_admin_user_id,is_impersonating,module,action,request_path,http_method,
                request_summary,status_code,ip_address,user_agent,elapsed_milliseconds,trace_id,created_at)
            VALUES(
                @AdminUserId,@DepartmentId,@ActorAdminUserId,@IsImpersonating,@Module,@Action,@RequestPath,@HttpMethod,
                @RequestSummary,@StatusCode,@IpAddress,@UserAgent,@ElapsedMilliseconds,@TraceId,UTC_TIMESTAMP())
            """;
        await using var db = await _factory.OpenConnectionAsync(ct);
        await db.ExecuteAsync(new CommandDefinition(sql, log, cancellationToken: ct));
    }

    public async Task<PagedResult<AuditLog>> GetPagedAsync(
        int pageIndex,
        int pageSize,
        string? keyword,
        string? module,
        DataScopeContext dataScope,
        CancellationToken ct = default)
    {
        pageIndex = Math.Max(1, pageIndex);
        pageSize = Math.Clamp(pageSize, 1, 100);
        var where = " WHERE 1=1";
        if (!string.IsNullOrWhiteSpace(keyword))
            where += " AND (request_path LIKE @keyword OR trace_id LIKE @keyword OR ip_address LIKE @keyword)";
        if (!string.IsNullOrWhiteSpace(module))
            where += " AND module=@module";
        if (!dataScope.IsSuperAdmin && dataScope.ScopeType != DataScopeType.All)
        {
            where += dataScope.ScopeType == DataScopeType.Self || dataScope.DepartmentIds.Count == 0
                ? " AND (admin_user_id=@scopeAdminId OR actor_admin_user_id=@scopeAdminId)"
                : " AND (admin_user_id=@scopeAdminId OR actor_admin_user_id=@scopeAdminId OR department_id IN @scopeDepartmentIds)";
        }

        var parameters = new
        {
            keyword = $"%{keyword}%",
            module,
            scopeAdminId = dataScope.AdminId,
            scopeDepartmentIds = dataScope.DepartmentIds,
            offset = (pageIndex - 1) * pageSize,
            pageSize
        };

        await using var db = await _factory.OpenConnectionAsync(ct);
        var items = (await db.QueryAsync<AuditLog>(new CommandDefinition(
            $"SELECT * FROM sys_audit_log{where} ORDER BY id DESC LIMIT @offset,@pageSize",
            parameters, cancellationToken: ct))).AsList();
        var total = await db.ExecuteScalarAsync<long>(new CommandDefinition(
            $"SELECT COUNT(*) FROM sys_audit_log{where}",
            parameters, cancellationToken: ct));
        return new PagedResult<AuditLog>(items, pageIndex, pageSize, total);
    }
}
