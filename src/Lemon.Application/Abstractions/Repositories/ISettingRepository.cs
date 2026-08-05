using Lemon.Domain.System;

namespace Lemon.Application.Abstractions.Repositories;

public interface ISettingRepository
{
    Task<IReadOnlyList<SystemSetting>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<SystemSetting?> GetByKeyAsync(string key, CancellationToken cancellationToken = default);
    Task UpsertAsync(SystemSetting setting, long operatorId, CancellationToken cancellationToken = default);
}
