using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace Common;

public class CacheManager<T>(IDistributedCache cache, ILogger logger, TimeSpan span, string cacheKey) where T : class
{
    public async Task CacheAsync(T data)
    {
        var serializedTags = JsonConvert.SerializeObject(data);
        var options = new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = span
        };
        await cache.SetStringAsync(cacheKey, serializedTags, options);
    }

    public async Task<T?> GetCacheAsync()
    {
        var cachedContent = await cache.GetStringAsync(cacheKey);
        if (string.IsNullOrEmpty(cachedContent)) return null;
        try
        {
            return JsonConvert.DeserializeObject<T>(cachedContent);
        }
        catch (JsonException ex)
        {
            logger.LogError(ex, "Failed to deserialize cached mod tags");
            return null;
        }
    }

    public async Task InvalidateCacheAsync()
    {
        try
        {
            await cache.RemoveAsync(cacheKey);
            logger.LogInformation("Cache invalidated for key {CacheKey}", cacheKey);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to invalidate cache for key {CacheKey}", cacheKey);
        }
    }
}