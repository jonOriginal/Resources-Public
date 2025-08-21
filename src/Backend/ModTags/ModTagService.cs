using Backend.Event;
using Backend.Event.EventModels;
using Backend.Event.Services;
using Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;

namespace Backend.ModTags;

public class ModTagService(
    IDistributedCache redis,
    IDbContextFactory<DataContext> ctxFactory,
    EventProducer eventProducer,
    ILogger<ModTagController> logger)
{
    private const string CacheKey = $"{nameof(Backend)}:{nameof(ModTagService)}:{nameof(ModTag)}";

    private ModTagRepo Repo { get; } = new(ctxFactory);

    private CacheManager<Dictionary<string, ModTag>> Cache { get; } =
        new(redis, logger, TimeSpan.FromMinutes(30), CacheKey);

    public async Task<List<ModTag>> GetAll()
    {
        var cachedTags = await Cache.GetCacheAsync();
        if (cachedTags != null) return cachedTags.Values.ToList();

        var tags = await Repo.GetAll();
        await Cache.CacheAsync(tags.ToDictionary(tag => tag.Id, tag => tag));
        return tags;
    }

    public async Task<ModTag?> GetModTag(string id)
    {
        var cachedTag = await Cache.GetCacheAsync();
        if (cachedTag != null && cachedTag.TryGetValue(id, out var tag)) return tag;
        return await Repo.Get(id);
    }

    public async Task CreateModTag(ModTag tag)
    {
        tag.CreatedAt = DateTime.UtcNow;
        tag.UpdatedAt = DateTime.UtcNow;

        await Repo.Create(tag);
        await eventProducer.PublishAsync(Streams.ModTags, new ModTagEvent
        {
            ModTagId = tag.Id,
            EventType = ModTagEventType.Create
        });
        await Cache.InvalidateCacheAsync();
    }

    public async Task UpdateModTag(ModTag tag)
    {
        tag.UpdatedAt = DateTime.UtcNow;
        await Repo.Update(tag);
        await eventProducer.PublishAsync(Streams.ModTags, new ModTagEvent
        {
            ModTagId = tag.Id,
            EventType = ModTagEventType.Update
        });
        await Cache.InvalidateCacheAsync();
    }

    public async Task DeleteModTag(string id)
    {
        await Repo.Delete(id);
        await eventProducer.PublishAsync(Streams.ModTags, new ModTagEvent
        {
            ModTagId = id,
            EventType = ModTagEventType.Delete
        });
        await Cache.InvalidateCacheAsync();
    }
}