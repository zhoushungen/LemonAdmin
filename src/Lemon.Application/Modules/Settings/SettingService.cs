using Lemon.Application.Abstractions.Cache;
using Lemon.Application.Abstractions.Repositories;
using Lemon.Application.Common;
using Lemon.Domain.System;

namespace Lemon.Application.Modules.Settings;

public sealed class SettingService : ISettingService
{
    private const string AllSettingsKey = "settings:all";
    private readonly ISettingRepository _repository;
    private readonly ICacheService _cache;

    public SettingService(ISettingRepository repository, ICacheService cache)
    {
        _repository = repository;
        _cache = cache;
    }

    public async Task<IReadOnlyList<SystemSettingDto>> GetAllAsync(CancellationToken ct = default)
    {
        var cached = await _cache.GetAsync<IReadOnlyList<SystemSettingDto>>(AllSettingsKey, ct);
        if (cached is not null) return cached;

        var data = (await _repository.GetAllAsync(ct)).Select(ToDto).ToArray();
        await _cache.SetAsync(AllSettingsKey, data, TimeSpan.FromMinutes(10), ct);
        return data;
    }

    public async Task<string?> GetValueAsync(string key, CancellationToken ct = default) =>
        (await _repository.GetByKeyAsync(key, ct))?.SettingValue;

    public async Task<SystemFeatureFlags> GetFeatureFlagsAsync(CancellationToken ct = default)
    {
        var settings = await GetAllAsync(ct);
        return new SystemFeatureFlags(
            GetBoolean(settings, "security.account_switch_enabled", false),
            GetBoolean(settings, "ui.theme_switch_enabled", true),
            GetBoolean(settings, "ui.font_size_switch_enabled", true));
    }

    public async Task UpsertAsync(string key, UpsertSettingRequest request, long operatorId, CancellationToken ct = default)
    {
        key = key.Trim();
        if (!IsValidKey(key))
            throw new AppException(ErrorCodes.ValidationFailed, "配置键只能包含字母、数字、点、下划线和短横线，且长度不能超过 120");

        await _repository.UpsertAsync(new SystemSetting
        {
            SettingGroup = request.SettingGroup.Trim(),
            SettingKey = key,
            SettingValue = request.SettingValue.Trim(),
            ValueType = request.ValueType.Trim(),
            Description = request.Description?.Trim(),
            IsPublic = request.IsPublic,
            IsEncrypted = false
        }, operatorId, ct);

        await _cache.RemoveAsync(AllSettingsKey, ct);
    }

    private static bool IsValidKey(string key) =>
        key.Length is > 0 and <= 120 &&
        key.All(character => char.IsLetterOrDigit(character) || character is '.' or '_' or '-');

    private static bool GetBoolean(IEnumerable<SystemSettingDto> settings, string key, bool defaultValue)
    {
        var value = settings.FirstOrDefault(x =>
            string.Equals(x.SettingKey, key, StringComparison.OrdinalIgnoreCase))?.SettingValue;
        return bool.TryParse(value, out var parsed) ? parsed : defaultValue;
    }

    private static SystemSettingDto ToDto(SystemSetting setting) => new(
        setting.Id,
        setting.SettingGroup,
        setting.SettingKey,
        setting.SettingValue,
        setting.ValueType,
        setting.Description,
        setting.IsEncrypted,
        setting.IsPublic,
        setting.UpdatedAt);
}
