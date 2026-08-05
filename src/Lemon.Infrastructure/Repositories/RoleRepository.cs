using Dapper;
using Lemon.Application.Abstractions.Database;
using Lemon.Application.Abstractions.Repositories;
using Lemon.Domain.System;

namespace Lemon.Infrastructure.Repositories;

public sealed class RoleRepository : IRoleRepository
{
    private readonly IDbConnectionFactory _factory;
    public RoleRepository(IDbConnectionFactory factory) => _factory = factory;

    public async Task<IReadOnlyList<Role>> GetAllAsync(CancellationToken ct = default)
    {
        await using var db = await _factory.OpenConnectionAsync(ct);
        return (await db.QueryAsync<Role>(new CommandDefinition(
            "SELECT * FROM sys_role ORDER BY is_system DESC,id",
            cancellationToken: ct))).AsList();
    }

    public async Task<Role?> GetByIdAsync(long id, CancellationToken ct = default)
    {
        await using var db = await _factory.OpenConnectionAsync(ct);
        return await db.QuerySingleOrDefaultAsync<Role>(new CommandDefinition(
            "SELECT * FROM sys_role WHERE id=@id LIMIT 1",
            new { id }, cancellationToken: ct));
    }

    public async Task<IReadOnlyList<PermissionItem>> GetPermissionsAsync(CancellationToken ct = default)
    {
        await using var db = await _factory.OpenConnectionAsync(ct);
        return (await db.QueryAsync<PermissionItem>(new CommandDefinition(
            "SELECT id,code,name,module FROM sys_permission WHERE is_enabled=1 ORDER BY module,id",
            cancellationToken: ct))).AsList();
    }

    public async Task<IReadOnlyCollection<long>> GetPermissionIdsAsync(long roleId, CancellationToken ct = default)
    {
        await using var db = await _factory.OpenConnectionAsync(ct);
        return (await db.QueryAsync<long>(new CommandDefinition(
            "SELECT permission_id FROM sys_role_permission WHERE role_id=@roleId ORDER BY permission_id",
            new { roleId }, cancellationToken: ct))).AsList();
    }

    public async Task<long> CreateAsync(Role role, IReadOnlyCollection<long> permissionIds, CancellationToken ct = default)
    {
        await using var db = await _factory.OpenConnectionAsync(ct);
        await using var transaction = await db.BeginTransactionAsync(ct);
        try
        {
            const string sql = """
                INSERT INTO sys_role(code,name,description,data_scope,is_system,is_enabled,created_by,created_at)
                VALUES(@Code,@Name,@Description,@DataScope,@IsSystem,@IsEnabled,@CreatedBy,UTC_TIMESTAMP());
                SELECT LAST_INSERT_ID();
                """;
            var id = await db.ExecuteScalarAsync<long>(new CommandDefinition(sql, role, transaction, cancellationToken: ct));
            await InsertPermissionsAsync(db, transaction, id, permissionIds, ct);
            await transaction.CommitAsync(ct);
            return id;
        }
        catch
        {
            await transaction.RollbackAsync(ct);
            throw;
        }
    }

    public async Task UpdateAsync(Role role, CancellationToken ct = default)
    {
        const string sql = """
            UPDATE sys_role
            SET name=@Name,description=@Description,data_scope=@DataScope,is_enabled=@IsEnabled,
                updated_by=@UpdatedBy,updated_at=UTC_TIMESTAMP()
            WHERE id=@Id
            """;
        await using var db = await _factory.OpenConnectionAsync(ct);
        await db.ExecuteAsync(new CommandDefinition(sql, role, cancellationToken: ct));
    }

    public async Task UpdatePermissionsAsync(long roleId, IReadOnlyCollection<long> permissionIds, long operatorId, CancellationToken ct = default)
    {
        await using var db = await _factory.OpenConnectionAsync(ct);
        await using var transaction = await db.BeginTransactionAsync(ct);
        try
        {
            await db.ExecuteAsync(new CommandDefinition(
                "DELETE FROM sys_role_permission WHERE role_id=@roleId",
                new { roleId }, transaction, cancellationToken: ct));
            await InsertPermissionsAsync(db, transaction, roleId, permissionIds, ct);
            await db.ExecuteAsync(new CommandDefinition(
                "UPDATE sys_role SET updated_by=@operatorId,updated_at=UTC_TIMESTAMP() WHERE id=@roleId",
                new { roleId, operatorId }, transaction, cancellationToken: ct));
            await transaction.CommitAsync(ct);
        }
        catch
        {
            await transaction.RollbackAsync(ct);
            throw;
        }
    }

    public async Task<bool> CodeExistsAsync(string code, long? excludeId = null, CancellationToken ct = default)
    {
        await using var db = await _factory.OpenConnectionAsync(ct);
        return await db.ExecuteScalarAsync<bool>(new CommandDefinition(
            "SELECT EXISTS(SELECT 1 FROM sys_role WHERE code=@code AND (@excludeId IS NULL OR id<>@excludeId))",
            new { code, excludeId }, cancellationToken: ct));
    }

    private static async Task InsertPermissionsAsync(
        System.Data.Common.DbConnection db,
        System.Data.Common.DbTransaction transaction,
        long roleId,
        IReadOnlyCollection<long> permissionIds,
        CancellationToken ct)
    {
        if (permissionIds.Count == 0) return;
        const string sql = "INSERT IGNORE INTO sys_role_permission(role_id,permission_id,created_at) VALUES(@RoleId,@PermissionId,UTC_TIMESTAMP())";
        await db.ExecuteAsync(new CommandDefinition(
            sql,
            permissionIds.Distinct().Select(id => new { RoleId = roleId, PermissionId = id }),
            transaction,
            cancellationToken: ct));
    }
}
