using Dapper;
using Lemon.Application.Abstractions.Database;
using Lemon.Application.Abstractions.Repositories;
using Lemon.Domain.System;

namespace Lemon.Infrastructure.Repositories;

public sealed class MenuRepository : IMenuRepository
{
    private readonly IDbConnectionFactory _factory;
    public MenuRepository(IDbConnectionFactory factory) => _factory = factory;

    public async Task<IReadOnlyList<Menu>> GetAllAsync(CancellationToken ct = default)
    {
        await using var db = await _factory.OpenConnectionAsync(ct);
        return (await db.QueryAsync<Menu>(new CommandDefinition(
            "SELECT * FROM sys_menu WHERE deleted_at IS NULL ORDER BY sort,id",
            cancellationToken: ct))).AsList();
    }

    public async Task<IReadOnlyList<Menu>> GetAuthorizedAsync(long adminId, bool superAdmin, CancellationToken ct = default)
    {
        await using var db = await _factory.OpenConnectionAsync(ct);
        if (superAdmin)
        {
            return (await db.QueryAsync<Menu>(new CommandDefinition(
                "SELECT * FROM sys_menu WHERE deleted_at IS NULL AND is_enabled=1 AND is_visible=1 ORDER BY sort,id",
                cancellationToken: ct))).AsList();
        }

        const string sql = """
            SELECT DISTINCT m.*
            FROM sys_menu m
            JOIN sys_admin_user a ON a.id=@adminId AND a.deleted_at IS NULL
            LEFT JOIN sys_permission p ON p.code=m.permission_code AND p.is_enabled=1
            LEFT JOIN sys_role_permission rp ON rp.permission_id=p.id AND rp.role_id=a.role_id
            WHERE m.deleted_at IS NULL
              AND m.is_enabled=1
              AND m.is_visible=1
              AND (m.permission_code IS NULL OR m.permission_code='' OR rp.role_id IS NOT NULL)
            ORDER BY m.sort,m.id
            """;
        return (await db.QueryAsync<Menu>(new CommandDefinition(sql, new { adminId }, cancellationToken: ct))).AsList();
    }

    public async Task<Menu?> GetByIdAsync(long id, CancellationToken ct = default)
    {
        await using var db = await _factory.OpenConnectionAsync(ct);
        return await db.QuerySingleOrDefaultAsync<Menu>(new CommandDefinition(
            "SELECT * FROM sys_menu WHERE id=@id AND deleted_at IS NULL",
            new { id }, cancellationToken: ct));
    }

    public async Task<long> CreateAsync(Menu menu, CancellationToken ct = default)
    {
        const string sql = """
            INSERT INTO sys_menu(parent_id,name,menu_type,route_name,route_path,component,icon,permission_code,sort,is_visible,is_enabled,created_by,created_at)
            VALUES(@ParentId,@Name,@MenuType,@RouteName,@RoutePath,@Component,@Icon,@PermissionCode,@Sort,@IsVisible,@IsEnabled,@CreatedBy,UTC_TIMESTAMP());
            SELECT LAST_INSERT_ID();
            """;
        await using var db = await _factory.OpenConnectionAsync(ct);
        return await db.ExecuteScalarAsync<long>(new CommandDefinition(sql, menu, cancellationToken: ct));
    }

    public async Task UpdateAsync(Menu menu, CancellationToken ct = default)
    {
        await using var db = await _factory.OpenConnectionAsync(ct);
        await db.ExecuteAsync(new CommandDefinition(
            "UPDATE sys_menu SET parent_id=@ParentId,name=@Name,menu_type=@MenuType,route_name=@RouteName,route_path=@RoutePath,component=@Component,icon=@Icon,permission_code=@PermissionCode,sort=@Sort,is_visible=@IsVisible,is_enabled=@IsEnabled,updated_by=@UpdatedBy,updated_at=UTC_TIMESTAMP() WHERE id=@Id AND deleted_at IS NULL",
            menu, cancellationToken: ct));
    }

    public async Task DeleteAsync(long id, long operatorId, CancellationToken ct = default)
    {
        await using var db = await _factory.OpenConnectionAsync(ct);
        await db.ExecuteAsync(new CommandDefinition(
            "UPDATE sys_menu SET deleted_at=UTC_TIMESTAMP(),updated_by=@operatorId WHERE id=@id",
            new { id, operatorId }, cancellationToken: ct));
    }

    public async Task<bool> HasChildrenAsync(long id, CancellationToken ct = default)
    {
        await using var db = await _factory.OpenConnectionAsync(ct);
        return await db.ExecuteScalarAsync<bool>(new CommandDefinition(
            "SELECT EXISTS(SELECT 1 FROM sys_menu WHERE parent_id=@id AND deleted_at IS NULL)",
            new { id }, cancellationToken: ct));
    }
}
