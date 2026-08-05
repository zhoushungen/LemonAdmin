using System.Text.Json;
using Lemon.Application.Abstractions.Cache;
using Lemon.Infrastructure.Options;
using Microsoft.Extensions.Options;
using StackExchange.Redis;

namespace Lemon.Infrastructure.Cache;

public sealed class RedisCacheService : ICacheService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly IDatabase _database;
    private readonly string _prefix;

    public RedisCacheService(IConnectionMultiplexer multiplexer, IOptions<RedisOptions> options)
    {
        _database = multiplexer.GetDatabase();
        _prefix = options.Value.InstanceName;
    }

    public async Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default)
    {
        var value = await _database.StringGetAsync(_prefix + key);
        return value.IsNullOrEmpty ? default : JsonSerializer.Deserialize<T>(value!, JsonOptions);
    }

    public Task SetAsync<T>(string key, T value, TimeSpan expiration, CancellationToken cancellationToken = default) =>
        _database.StringSetAsync(_prefix + key, JsonSerializer.Serialize(value, JsonOptions), expiration);

    public Task RemoveAsync(string key, CancellationToken cancellationToken = default) =>
        _database.KeyDeleteAsync(_prefix + key);

    public Task<bool> ExistsAsync(string key, CancellationToken cancellationToken = default) =>
        _database.KeyExistsAsync(_prefix + key);
}
