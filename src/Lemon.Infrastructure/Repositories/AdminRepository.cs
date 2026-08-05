using Dapper;
using Lemon.Application.Abstractions.Database;
using Lemon.Application.Abstractions.Repositories;
using Lemon.Application.Abstractions.Security;
using Lemon.Domain.System;
using Lemon.Application.Common;

namespace Lemon.Infrastructure.Repositories;

public sealed class AdminRepository : IAdminRepository
{
    private readonly IDbConnectionFactory _factory;
    public AdminRepository(IDbConnectionFactory factory) => _factory = factory;

    public async Task<AdminUser?> GetByIdAsync(long id, CancellationToken ct = default)
    {
        await using var db = await _factory.OpenConnectionAsync(ct);
        return await db.QuerySingleOrDefaultAsync<AdminUser>(new CommandDefinition(
            "SELECT * FROM sys_admin_user WHERE id=@id AND deleted_at IS NULL LIMIT 1",
            new { id }, cancellationToken: ct));
    }

    public async Task<AdminUser?> GetByUsernameAsync(string username, CancellationToken ct = default)
    {
        await using var db = await _factory.OpenConnectionAsync(ct);
        return await db.QuerySingleOrDefaultAsync<AdminUser>(new CommandDefinition(
            "SELECT * FROM sys_admin_user WHERE username=@username AND deleted_at IS NULL LIMIT 1",
            new { username }, cancellationToken: ct));
    }

    public async Task<AdminAccessProfile?> GetAccessProfileAsync(long id, CancellationToken ct = default)
    {
        const string sql = """
            SELECT a.id,a.department_id,a.role_id,
                   COALESCE(r.data_scope,5) AS data_scope,
                   a.is_enabled,
                   CASE WHEN a.role_id IS NULL THEN 1 ELSE COALESCE(r.is_enabled,0) END AS role_is_enabled,
                   CASE WHEN a.role_id IS NULL THEN 1 ELSE COALESCE(d.is_enabled,0) END AS department_is_enabled
            FROM sys_admin_user a
            LEFT JOIN sys_role r ON r.id=a.role_id
            LEFT JOIN sys_department d ON d.id=a.department_id AND d.deleted_at IS NULL
            WHERE a.id=@id AND a.deleted_at IS NULL
            LIMIT 1
            """;
        await using var db = await _factory.OpenConnectionAsync(ct);
        return await db.QuerySingleOrDefaultAsync<AdminAccessProfile>(
            new CommandDefinition(sql, new { id }, cancellationToken: ct));
    }

    public async Task<PagedResult<AdminListItem>> GetPagedAsync(
        int pageIndex,
        int pageSize,
        string? keyword,
        long? departmentId,
        bool? enabled,
        DataScopeContext dataScope,
        CancellationToken ct = default)
    {
        pageIndex = Math.Max(1, pageIndex);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var where = " WHERE a.deleted_at IS NULL ";
        if (!string.IsNullOrWhiteSpace(keyword))
            where += " AND (a.username LIKE @keyword OR a.display_name LIKE @keyword OR a.mobile LIKE @keyword)";
        if (departmentId.HasValue)
            where += " AND a.department_id=@departmentId";
        if (enabled.HasValue)
            where += " AND a.is_enabled=@enabled";

        if (!dataScope.IsSuperAdmin && dataScope.ScopeType != DataScopeType.All)
        {
            where += dataScope.ScopeType == DataScopeType.Self || dataScope.DepartmentIds.Count == 0
                ? " AND a.id=@scopeAdminId"
                : " AND (a.id=@scopeAdminId OR a.department_id IN @scopeDepartmentIds)";
        }

        const string from = " FROM sys_admin_user a LEFT JOIN sys_department d ON d.id=a.department_id LEFT JOIN sys_role r ON r.id=a.role_id ";
        var listSql = $"""
            SELECT a.id,a.department_id,d.name AS department_name,a.role_id,r.name AS role_name,
                   a.username,a.display_name,a.email,a.mobile,a.is_enabled,
                   CASE WHEN a.role_id IS NULL THEN 1 ELSE 0 END AS is_super_admin,
                   a.last_login_at,a.last_login_ip,a.created_at
            {from}{where}
            ORDER BY a.id DESC
            LIMIT @offset,@pageSize
            """;
        var countSql = $"SELECT COUNT(*) {from}{where}";
        var parameters = new
        {
            keyword = $"%{keyword}%",
            departmentId,
            enabled,
            scopeAdminId = dataScope.AdminId,
            scopeDepartmentIds = dataScope.DepartmentIds,
            offset = (pageIndex - 1) * pageSize,
            pageSize
        };

        await using var db = await _factory.OpenConnectionAsync(ct);
        var items = (await db.QueryAsync<AdminListItem>(new CommandDefinition(listSql, parameters, cancellationToken: ct))).AsList();
        var total = await db.ExecuteScalarAsync<long>(new CommandDefinition(countSql, parameters, cancellationToken: ct));
        return new PagedResult<AdminListItem>(items, pageIndex, pageSize, total);
    }

    public async Task<IReadOnlyList<AdminOption>> GetOptionsAsync(DataScopeContext dataScope, CancellationToken ct = default)
    {
        var where = " WHERE a.deleted_at IS NULL AND a.is_enabled=1 AND a.role_id IS NOT NULL";
        if (!dataScope.IsSuperAdmin && dataScope.ScopeType != DataScopeType.All)
        {
            where += dataScope.ScopeType == DataScopeType.Self || dataScope.DepartmentIds.Count == 0
                ? " AND a.id=@scopeAdminId"
                : " AND (a.id=@scopeAdminId OR a.department_id IN @scopeDepartmentIds)";
        }

        var sql = $"""
            SELECT a.id,a.display_name,a.username,a.department_id
            FROM sys_admin_user a
            {where}
            ORDER BY a.display_name,a.id
            """;
        await using var db = await _factory.OpenConnectionAsync(ct);
        return (await db.QueryAsync<AdminOption>(new CommandDefinition(
            sql,
            new { scopeAdminId = dataScope.AdminId, scopeDepartmentIds = dataScope.DepartmentIds },
            cancellationToken: ct))).AsList();
    }

    public async Task<long> CreateAsync(AdminUser admin, CancellationToken ct = default)
    {
        const string sql = """
            INSERT INTO sys_admin_user(
                department_id,role_id,username,display_name,password_hash,password_salt,
                email,mobile,is_enabled,created_by,created_at)
            VALUES(
                @DepartmentId,@RoleId,@Username,@DisplayName,@PasswordHash,@PasswordSalt,
                @Email,@Mobile,@IsEnabled,@CreatedBy,UTC_TIMESTAMP());
            SELECT LAST_INSERT_ID();
            """;
        await using var db = await _factory.OpenConnectionAsync(ct);
        return await db.ExecuteScalarAsync<long>(new CommandDefinition(sql, admin, cancellationToken: ct));
    }

    public async Task UpdateAsync(AdminUser admin, CancellationToken ct = default)
    {
        const string sql = """
            UPDATE sys_admin_user
            SET department_id=@DepartmentId,role_id=@RoleId,display_name=@DisplayName,
                email=@Email,mobile=@Mobile,is_enabled=@IsEnabled,
                updated_by=@UpdatedBy,updated_at=UTC_TIMESTAMP()
            WHERE id=@Id AND deleted_at IS NULL
            """;
        await using var db = await _factory.OpenConnectionAsync(ct);
        await db.ExecuteAsync(new CommandDefinition(sql, admin, cancellationToken: ct));
    }

    public async Task UpdateStatusAsync(long id, bool enabled, long operatorId, CancellationToken ct = default)
    {
        await using var db = await _factory.OpenConnectionAsync(ct);
        await db.ExecuteAsync(new CommandDefinition(
            "UPDATE sys_admin_user SET is_enabled=@enabled,updated_by=@operatorId,updated_at=UTC_TIMESTAMP() WHERE id=@id AND deleted_at IS NULL",
            new { id, enabled, operatorId }, cancellationToken: ct));
    }

    public async Task UpdateLoginSuccessAsync(long id, string? ip, CancellationToken ct = default)
    {
        await using var db = await _factory.OpenConnectionAsync(ct);
        await db.ExecuteAsync(new CommandDefinition(
            "UPDATE sys_admin_user SET failed_login_count=0,locked_until=NULL,last_login_at=UTC_TIMESTAMP(),last_login_ip=@ip WHERE id=@id",
            new { id, ip }, cancellationToken: ct));
    }

    public async Task UpdateLoginFailureAsync(long id, int failedCount, DateTime? lockedUntil, CancellationToken ct = default)
    {
        await using var db = await _factory.OpenConnectionAsync(ct);
        await db.ExecuteAsync(new CommandDefinition(
            "UPDATE sys_admin_user SET failed_login_count=@failedCount,locked_until=@lockedUntil WHERE id=@id",
            new { id, failedCount, lockedUntil }, cancellationToken: ct));
    }

    public async Task<IReadOnlyCollection<string>> GetPermissionsAsync(long adminId, CancellationToken ct = default)
    {
        const string sql = """
            SELECT DISTINCT p.code
            FROM sys_admin_user a
            JOIN sys_role r ON r.id=a.role_id AND r.is_enabled=1
            JOIN sys_role_permission rp ON rp.role_id=r.id
            JOIN sys_permission p ON p.id=rp.permission_id AND p.is_enabled=1
            WHERE a.id=@adminId AND a.deleted_at IS NULL
            """;
        await using var db = await _factory.OpenConnectionAsync(ct);
        return (await db.QueryAsync<string>(new CommandDefinition(sql, new { adminId }, cancellationToken: ct))).AsList();
    }

    public async Task<IReadOnlyCollection<long>> GetAdminIdsByRoleAsync(long roleId, CancellationToken ct = default)
    {
        await using var db = await _factory.OpenConnectionAsync(ct);
        return (await db.QueryAsync<long>(new CommandDefinition(
            "SELECT id FROM sys_admin_user WHERE role_id=@roleId AND deleted_at IS NULL",
            new { roleId }, cancellationToken: ct))).AsList();
    }

    public async Task<bool> IsSuperAdminAsync(long adminId, CancellationToken ct = default)
    {
        await using var db = await _factory.OpenConnectionAsync(ct);
        return await db.ExecuteScalarAsync<bool>(new CommandDefinition(
            "SELECT EXISTS(SELECT 1 FROM sys_admin_user WHERE id=@adminId AND role_id IS NULL AND is_enabled=1 AND deleted_at IS NULL)",
            new { adminId }, cancellationToken: ct));
    }

    public async Task<bool> UsernameExistsAsync(string username, long? excludeId = null, CancellationToken ct = default)
    {
        await using var db = await _factory.OpenConnectionAsync(ct);
        return await db.ExecuteScalarAsync<bool>(new CommandDefinition(
            "SELECT EXISTS(SELECT 1 FROM sys_admin_user WHERE username=@username AND deleted_at IS NULL AND (@excludeId IS NULL OR id<>@excludeId))",
            new { username, excludeId }, cancellationToken: ct));
    }

    public async Task<int> CountAsync(CancellationToken ct = default)
    {
        await using var db = await _factory.OpenConnectionAsync(ct);
        return await db.ExecuteScalarAsync<int>(new CommandDefinition(
            "SELECT COUNT(*) FROM sys_admin_user WHERE deleted_at IS NULL",
            cancellationToken: ct));
    }
}
