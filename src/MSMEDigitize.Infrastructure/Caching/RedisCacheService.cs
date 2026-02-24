using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;
using MSMEDigitize.Core.Interfaces;

namespace MSMEDigitize.Infrastructure.Caching;

public class RedisCacheService : ICacheService
{
    private readonly IDistributedCache _cache;
    private static readonly JsonSerializerOptions _jsonOptions = new() { PropertyNameCaseInsensitive = true };

    public RedisCacheService(IDistributedCache cache) => _cache = cache;

    public async Task<T?> GetAsync<T>(string key, CancellationToken ct = default)
    {
        var data = await _cache.GetStringAsync(key, ct);
        return data == null ? default : JsonSerializer.Deserialize<T>(data, _jsonOptions);
    }

    public async Task SetAsync<T>(string key, T value, TimeSpan? expiry = null, CancellationToken ct = default)
    {
        var options = new DistributedCacheEntryOptions();
        if (expiry.HasValue) options.AbsoluteExpirationRelativeToNow = expiry;
        else options.SlidingExpiration = TimeSpan.FromMinutes(30);
        await _cache.SetStringAsync(key, JsonSerializer.Serialize(value, _jsonOptions), options, ct);
    }

    public async Task RemoveAsync(string key, CancellationToken ct = default)
        => await _cache.RemoveAsync(key, ct);

    public async Task RemoveByPatternAsync(string pattern, CancellationToken ct = default)
        => await Task.CompletedTask; // Placeholder

    public async Task<T> GetOrSetAsync<T>(string key, Func<Task<T>> factory, TimeSpan? expiry = null, CancellationToken ct = default)
    {
        var cached = await GetAsync<T>(key, ct);
        if (cached != null) return cached;
        var value = await factory();
        await SetAsync(key, value, expiry, ct);
        return value;
    }

    public async Task<bool> ExistsAsync(string key) => await _cache.GetAsync(key) != null;
}