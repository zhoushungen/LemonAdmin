using Lemon.Application.Abstractions.Security;
using Lemon.Domain.System;
using Lemon.Application.Common;

namespace Lemon.Application.Abstractions.Repositories;

public sealed class AdminListItem
{
    public long Id { get; set; }
    public long? DepartmentId { get; set; }
    public string? DepartmentName { get; set; }
    public long? RoleId { get; set; }
    public string? RoleName { get; set; }
    public string Username { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? Mobile { get; set; }
    public bool IsEnabled { get; set; }
    public bool IsSuperAdmin { get; set; }
    public DateTime? LastLoginAt { get; set; }
    public string? LastLoginIp { get; set; }
    public DateTime CreatedAt { get; set; }
}

public sealed class AdminOption
{
    public long Id { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public long? DepartmentId { get; set; }
}

public sealed class AdminAccessProfile
{
    public long Id { get; set; }
    public long? DepartmentId { get; set; }
    public long? RoleId { get; set; }
    public DataScopeType DataScope { get; set; }
    public bool IsEnabled { get; set; }
    public bool RoleIsEnabled { get; set; }
    public bool DepartmentIsEnabled { get; set; }
}

public interface IAdminRepository
{
    Task<AdminUser?> GetByIdAsync(long id, CancellationToken cancellationToken = default);
    Task<AdminUser?> GetByUsernameAsync(string username, CancellationToken cancellationToken = default);
    Task<AdminAccessProfile?> GetAccessProfileAsync(long id, CancellationToken cancellationToken = default);
    Task<PagedResult<AdminListItem>> GetPagedAsync(int pageIndex, int pageSize, string? keyword, long? departmentId, bool? enabled, DataScopeContext dataScope, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<AdminOption>> GetOptionsAsync(DataScopeContext dataScope, CancellationToken cancellationToken = default);
    Task<long> CreateAsync(AdminUser admin, CancellationToken cancellationToken = default);
    Task UpdateAsync(AdminUser admin, CancellationToken cancellationToken = default);
    Task UpdateStatusAsync(long id, bool enabled, long operatorId, CancellationToken cancellationToken = default);
    Task UpdateLoginSuccessAsync(long id, string? ip, CancellationToken cancellationToken = default);
    Task UpdateLoginFailureAsync(long id, int failedCount, DateTime? lockedUntil, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<string>> GetPermissionsAsync(long adminId, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<long>> GetAdminIdsByRoleAsync(long roleId, CancellationToken cancellationToken = default);
    Task<bool> IsSuperAdminAsync(long adminId, CancellationToken cancellationToken = default);
    Task<bool> UsernameExistsAsync(string username, long? excludeId = null, CancellationToken cancellationToken = default);
    Task<int> CountAsync(CancellationToken cancellationToken = default);
}
