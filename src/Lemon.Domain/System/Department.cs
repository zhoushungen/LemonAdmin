using Lemon.Domain.Common;

namespace Lemon.Domain.System;

public sealed class Department : Entity
{
    public long? ParentId { get; set; }
    public long? ManagerAdminId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public int Sort { get; set; }
    public bool IsEnabled { get; set; }
}
