using Dapper;
using Lemon.Application.Abstractions.Database;
using Lemon.Application.Abstractions.Repositories;
using Lemon.Domain.System;

namespace Lemon.Infrastructure.Repositories;

public sealed class DepartmentRepository : IDepartmentRepository
{
    private readonly IDbConnectionFactory _factory;
    public DepartmentRepository(IDbConnectionFactory factory) => _factory = factory;

    public async Task<IReadOnlyList<DepartmentListItem>> GetAllAsync(CancellationToken ct = default)
    {
        const string sql = """
            SELECT d.id,d.parent_id,d.manager_admin_id,a.display_name AS manager_name,
                   d.name,d.code,d.phone,d.email,d.sort,d.is_enabled
            FROM sys_department d
            LEFT JOIN sys_admin_user a ON a.id=d.manager_admin_id AND a.deleted_at IS NULL
            WHERE d.deleted_at IS NULL
            ORDER BY d.sort,d.id
            """;
        await using var db = await _factory.OpenConnectionAsync(ct);
        return (await db.QueryAsync<DepartmentListItem>(new CommandDefinition(sql, cancellationToken: ct))).AsList();
    }

    public async Task<Department?> GetByIdAsync(long id, CancellationToken ct = default)
    {
        await using var db = await _factory.OpenConnectionAsync(ct);
        return await db.QuerySingleOrDefaultAsync<Department>(new CommandDefinition(
            "SELECT * FROM sys_department WHERE id=@id AND deleted_at IS NULL",
            new { id }, cancellationToken: ct));
    }

    public async Task<long> CreateAsync(Department department, CancellationToken ct = default)
    {
        const string sql = """
            INSERT INTO sys_department(parent_id,manager_admin_id,name,code,phone,email,sort,is_enabled,created_by,created_at)
            VALUES(@ParentId,@ManagerAdminId,@Name,@Code,@Phone,@Email,@Sort,@IsEnabled,@CreatedBy,UTC_TIMESTAMP());
            SELECT LAST_INSERT_ID();
            """;
        await using var db = await _factory.OpenConnectionAsync(ct);
        return await db.ExecuteScalarAsync<long>(new CommandDefinition(sql, department, cancellationToken: ct));
    }

    public async Task UpdateAsync(Department department, CancellationToken ct = default)
    {
        const string sql = """
            UPDATE sys_department
            SET parent_id=@ParentId,manager_admin_id=@ManagerAdminId,name=@Name,code=@Code,
                phone=@Phone,email=@Email,sort=@Sort,is_enabled=@IsEnabled,
                updated_by=@UpdatedBy,updated_at=UTC_TIMESTAMP()
            WHERE id=@Id AND deleted_at IS NULL
            """;
        await using var db = await _factory.OpenConnectionAsync(ct);
        await db.ExecuteAsync(new CommandDefinition(sql, department, cancellationToken: ct));
    }

    public async Task DeleteAsync(long id, long operatorId, CancellationToken ct = default)
    {
        await using var db = await _factory.OpenConnectionAsync(ct);
        await db.ExecuteAsync(new CommandDefinition(
            "UPDATE sys_department SET deleted_at=UTC_TIMESTAMP(),updated_by=@operatorId,updated_at=UTC_TIMESTAMP() WHERE id=@id AND deleted_at IS NULL",
            new { id, operatorId }, cancellationToken: ct));
    }

    public async Task<bool> CodeExistsAsync(string code, long? excludeId = null, CancellationToken ct = default)
    {
        await using var db = await _factory.OpenConnectionAsync(ct);
        return await db.ExecuteScalarAsync<bool>(new CommandDefinition(
            "SELECT EXISTS(SELECT 1 FROM sys_department WHERE code=@code AND deleted_at IS NULL AND (@excludeId IS NULL OR id<>@excludeId))",
            new { code, excludeId }, cancellationToken: ct));
    }

    public async Task<bool> HasChildrenOrAdminsAsync(long id, CancellationToken ct = default)
    {
        const string sql = """
            SELECT EXISTS(SELECT 1 FROM sys_department WHERE parent_id=@id AND deleted_at IS NULL)
                OR EXISTS(SELECT 1 FROM sys_admin_user WHERE department_id=@id AND deleted_at IS NULL)
            """;
        await using var db = await _factory.OpenConnectionAsync(ct);
        return await db.ExecuteScalarAsync<bool>(new CommandDefinition(sql, new { id }, cancellationToken: ct));
    }
}
