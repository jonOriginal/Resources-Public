using Backend.Event;
using Backend.Event.EventModels;
using Backend.Event.Services;
using Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;

namespace Backend.Mods;

public class ModService(
    IDbContextFactory<DataContext> ctxFactory,
    IDistributedCache cache,
    EventProducer eventProducer,
    ILogger<ModService> logger)
{
    private const string CacheKey = $"{nameof(Backend)}:{nameof(ModService)}:{nameof(Mod)}";
    private const string SearchCacheKey = $"{CacheKey}:search";

    private CacheManager<Dictionary<string, Mod>> Cache { get; } =
        new(cache, logger, TimeSpan.FromMinutes(30), CacheKey);

    private CacheManager<Dictionary<string, List<Mod>>> SearchCache { get; } =
        new(cache, logger, TimeSpan.FromMinutes(30), SearchCacheKey);

    private ModRepo Repo { get; } = new(ctxFactory);

    private static string GenerateSearchKey(string search, IEnumerable<string> tags)
    {
        var sanitizedSearch = search.Sanitize();
        var sanitizedTags = tags.Sanitize();
        var cacheKey = $"{SearchCacheKey}:{sanitizedSearch}:{string.Join(",", sanitizedTags)}";
        return cacheKey;
    }

    public async Task<ICollection<Mod>> SearchByAnyTags(IEnumerable<string> tags)
    {
        if (!tags.Any())
        {
            logger.LogWarning("No tags provided for search by any tags");
            return Array.Empty<Mod>();
        }

        var mods = await Repo.SearchByAnyTags(tags.ToArray());
        return mods;
    }

    public async Task<ICollection<Mod>> Search(string search, IEnumerable<string> tags)
    {
        var cacheKey = GenerateSearchKey(search, tags);
        var cached = await SearchCache.GetCacheAsync();
        if (cached != null && cached.TryGetValue(cacheKey, out var cachedMods))
        {
            logger.LogInformation("Cache hit for mods search with key {CacheKey}", cacheKey);
            return cachedMods;
        }

        logger.LogInformation("Cache miss for mods search with key {CacheKey}", cacheKey);
        var mods = await Repo.Search(search, tags.ToArray());

        logger.LogInformation("Caching mods search with key {CacheKey}", cacheKey);
        cached ??= new Dictionary<string, List<Mod>>();
        cached[cacheKey] = mods;
        await SearchCache.CacheAsync(cached);

        return mods;
    }

    public async Task<ICollection<Mod>> GetAll()
    {
        var cached = await Cache.GetCacheAsync();
        if (cached != null)
        {
            logger.LogInformation("Cache hit for mods");
            return cached.Values.ToList();
        }

        logger.LogInformation("Caching mods");
        var mods = await Repo.GetAll();
        await Cache.CacheAsync(mods.ToDictionary(m => m.Id));
        return mods;
    }

    public async Task<Mod?> Get(string id)
    {
        var cached = await Cache.GetCacheAsync();
        if (cached != null && cached.TryGetValue(id, out var cachedMod))
        {
            logger.LogInformation("Cache hit for mod {Id}", id);
            return cachedMod;
        }

        logger.LogInformation("Caching mod {Id}", id);
        return await Repo.Get(id);
    }

    public async Task Create(Mod mod)
    {
        mod.CreatedAt = DateTime.UtcNow;
        mod.UpdatedAt = DateTime.UtcNow;
        await eventProducer.PublishAsync(Streams.Mods, new ModEvent
        {
            ModId = mod.Id,
            EventType = ModEventType.Create
        });
        await Repo.Create(mod);
        await Cache.InvalidateCacheAsync();
        await SearchCache.InvalidateCacheAsync();
    }

    public async Task Update(Mod mod)
    {
        mod.UpdatedAt = DateTime.UtcNow;
        var existingMod = await Repo.Get(mod.Id);
        if (existingMod == null)
        {
            return;
        }
        
        await Repo.Update(mod);
        await eventProducer.PublishAsync(Streams.Mods, new ModEvent
        {
            ModId = mod.Id,
            EventType = ModEventType.Update
        });
        if (!existingMod.IsCompromised && mod.IsCompromised)
        {
            await eventProducer.PublishAsync(Streams.Mods, new ModEvent
            {
                ModId = mod.Id,
                EventType = ModEventType.Compromised
            });
        }
        await Cache.InvalidateCacheAsync();
        await SearchCache.InvalidateCacheAsync();
    }

    public async Task Delete(string id)
    {
        await Repo.Delete(id);
        await eventProducer.PublishAsync(Streams.Mods, new ModEvent
        {
            ModId = id,
            EventType = ModEventType.Delete
        });
        await Cache.InvalidateCacheAsync();
        await SearchCache.InvalidateCacheAsync();
    }
}