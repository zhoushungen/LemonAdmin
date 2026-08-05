namespace Lemon.Application.Modules.Settings;

public sealed record SystemSettingDto(
    long Id,
    string SettingGroup,
    string SettingKey,
    string SettingValue,
    string ValueType,
    string? Description,
    bool IsEncrypted,
    bool IsPublic,
    DateTime? UpdatedAt);

public sealed record UpsertSettingRequest(
    string SettingGroup,
    string SettingValue,
    string ValueType,
    string? Description,
    bool IsPublic);

public sealed record SystemFeatureFlags(
    bool AccountSwitchEnabled,
    bool ThemeSwitchEnabled,
    bool FontSizeSwitchEnabled);
