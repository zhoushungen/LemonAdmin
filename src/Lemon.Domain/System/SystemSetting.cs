using Lemon.Domain.Common;

namespace Lemon.Domain.System;

public sealed class SystemSetting : Entity
{
    public string SettingGroup { get; set; } = string.Empty;
    public string SettingKey { get; set; } = string.Empty;
    public string SettingValue { get; set; } = string.Empty;
    public string ValueType { get; set; } = "string";
    public string? Description { get; set; }
    public bool IsEncrypted { get; set; }
    public bool IsPublic { get; set; }
}
