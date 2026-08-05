namespace Lemon.Application.Modules.Settings;

public interface ISettingService
{
    Task<IReadOnlyList<SystemSettingDto>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<string?> GetValueAsync(string key, CancellationToken cancellationToken = default);
    Task<SystemFeatureFlags> GetFeatureFlagsAsync(CancellationToken cancellationToken = default);
    Task UpsertAsync(string key, UpsertSettingRequest request, long operatorId, CancellationToken cancellationToken = default);
}
