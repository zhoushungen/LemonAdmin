using Lemon.Domain.System;

namespace Lemon.Application.Modules.Roles;

public sealed record RoleDto(
    long Id,
    string Code,
    string Name,
    string? Description,
    DataScopeType DataScope,
    bool IsSystem,
    bool IsEnabled,
    IReadOnlyCollection<long> PermissionIds);

public sealed record PermissionDto(long Id, string Code, string Name, string Module);
public sealed record CreateRoleRequest(string Code, string Name, string? Description, DataScopeType DataScope, IReadOnlyCollection<long> PermissionIds);
public sealed record UpdateRoleRequest(string Name, string? Description, DataScopeType DataScope, bool IsEnabled);
public sealed record UpdateRolePermissionsRequest(IReadOnlyCollection<long> PermissionIds);
