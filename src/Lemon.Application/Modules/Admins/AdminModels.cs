namespace Lemon.Application.Modules.Admins;

public sealed record AdminDto(
    long Id,
    long? DepartmentId,
    string? DepartmentName,
    long? RoleId,
    string? RoleName,
    string Username,
    string DisplayName,
    string? Email,
    string? Mobile,
    bool IsSuperAdmin,
    bool IsEnabled,
    DateTime? LastLoginAt,
    string? LastLoginIp,
    DateTime CreatedAt);

public sealed record AdminDetailDto(
    long Id,
    long? DepartmentId,
    long? RoleId,
    string Username,
    string DisplayName,
    string? Email,
    string? Mobile,
    bool IsSuperAdmin,
    bool IsEnabled);

public sealed record AdminOptionDto(long Id, string DisplayName, string Username, long? DepartmentId);

public sealed record CreateAdminRequest(
    long? DepartmentId,
    long? RoleId,
    string Username,
    string DisplayName,
    string Password,
    string? Email,
    string? Mobile);

public sealed record UpdateAdminRequest(
    long? DepartmentId,
    long? RoleId,
    string DisplayName,
    string? Email,
    string? Mobile,
    bool IsEnabled);

public sealed record ChangeAdminStatusRequest(bool Enabled);
