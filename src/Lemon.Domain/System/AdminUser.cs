using Lemon.Domain.Common;

namespace Lemon.Domain.System;

public sealed class AdminUser : Entity
{
    public long? DepartmentId { get; set; }
    public long? RoleId { get; set; }
    public string Username { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string PasswordSalt { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? Mobile { get; set; }
    public bool IsEnabled { get; set; }
    public int FailedLoginCount { get; set; }
    public DateTime? LockedUntil { get; set; }
    public DateTime? LastLoginAt { get; set; }
    public string? LastLoginIp { get; set; }

    public bool IsSuperAdmin => RoleId is null;
}
