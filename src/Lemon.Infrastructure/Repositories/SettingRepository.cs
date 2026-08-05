using Lemon.Application.Abstractions.Database;
using Lemon.Application.Abstractions.Repositories;
using Lemon.Domain.System;
using Dapper;

namespace Lemon.Infrastructure.Repositories;

public sealed class SettingRepository : ISettingRepository
{
    private readonly IDbConnectionFactory _factory;
    public SettingRepository(IDbConnectionFactory factory) => _factory = factory;

    public async Task<IReadOnlyList<SystemSetting>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        await using var db = await _factory.OpenConnectionAsync(cancellationToken);
        return (await db.QueryAsync<SystemSetting>(new CommandDefinition("SELECT * FROM sys_setting ORDER BY setting_group,setting_key", cancellationToken: cancellationToken))).AsList();
    }

    public async Task<SystemSetting?> GetByKeyAsync(string key, CancellationToken cancellationToken = default)
    {
        await using var db = await _factory.OpenConnectionAsync(cancellationToken);
        return await db.QuerySingleOrDefaultAsync<SystemSetting>(new CommandDefinition("SELECT * FROM sys_setting WHERE setting_key=@Key LIMIT 1", new { Key = key }, cancellationToken: cancellationToken));
    }

    public async Task UpsertAsync(SystemSetting setting, long operatorId, CancellationToken cancellationToken = default)
    {
        const string sql = """
            INSERT INTO sys_setting(setting_group,setting_key,setting_value,value_type,description,is_encrypted,is_public,created_by,created_at)
            VALUES(@SettingGroup,@SettingKey,@SettingValue,@ValueType,@Description,@IsEncrypted,@IsPublic,@OperatorId,UTC_TIMESTAMP())
            ON DUPLICATE KEY UPDATE setting_group=VALUES(setting_group),setting_value=VALUES(setting_value),value_type=VALUES(value_type),
            description=VALUES(description),is_public=VALUES(is_public),updated_by=@OperatorId,updated_at=UTC_TIMESTAMP();
            """;
        await using var db = await _factory.OpenConnectionAsync(cancellationToken);
        await db.ExecuteAsync(new CommandDefinition(sql, new { setting.SettingGroup, setting.SettingKey, setting.SettingValue, setting.ValueType, setting.Description, setting.IsEncrypted, setting.IsPublic, OperatorId = operatorId }, cancellationToken: cancellationToken));
    }
}
