using Lemon.Domain.Common;

namespace Lemon.Domain.System;

public sealed class Role : Entity
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DataScopeType DataScope { get; set; } = DataScopeType.Self;
    public bool IsSystem { get; set; }
    public bool IsEnabled { get; set; }
}
