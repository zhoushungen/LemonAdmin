using Lemon.Application.Abstractions.Cache;
using Microsoft.Extensions.Caching.Memory;

namespace Lemon.Infrastructure.Cache;

public sealed class MemoryCacheService : ICacheService
{
    private readonly IMemoryCache _cache;
    public MemoryCacheService(IMemoryCache cache) => _cache = cache;

    public Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default) =>
        Task.FromResult(_cache.TryGetValue<T>(key, out var value) ? value : default);

    public Task SetAsync<T>(string key, T value, TimeSpan expiration, CancellationToken cancellationToken = default)
    {
        _cache.Set(key, value, expiration);
        return Task.CompletedTask;
    }

    public Task RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        _cache.Remove(key);
        return Task.CompletedTask;
    }

    public Task<bool> ExistsAsync(string key, CancellationToken cancellationToken = default) =>
        Task.FromResult(_cache.TryGetValue(key, out _));
}
