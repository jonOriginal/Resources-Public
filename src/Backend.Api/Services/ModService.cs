using Microsoft.Extensions.Logging;

namespace Backend.Api.Services;

public class ModService(BackendClient client, ILogger<ModService> logger)
{
    #region Create

    public async Task<Mod> CreateMod(Mod mod)
    {
        logger.LogInformation("Creating mod {modName}", mod.Name);
        return await client.ModPOSTAsync(mod);
    }

    public async Task<ModTag> CreateModTag(ModTag tag)
    {
        logger.LogInformation("Creating mod tag {tagName}", tag.Name);
        return await client.ModTagPOSTAsync(tag);
    }

    #endregion

    #region Retrieve

    public async Task<ICollection<Mod>> GetModsByAnyTags(string[] tags)
    {
        logger.LogInformation("Fetching mods from API with tags: {tags}", string.Join(",", tags));
        var mods = await client.AnyAsync(tags);
        return mods;
    }

    public async Task<ICollection<Mod>> GetMods()
    {
        logger.LogInformation("Fetching all mods from API.");
        var mods = await client.List3Async();
        return mods;
    }

    public async Task<IDictionary<string, Mod>> GetModDictionary()
    {
        var mods = await GetMods();
        return mods.ToDictionary(m => m.Id, m => m);
    }

    public async Task<ICollection<ModTag>> GetTags()
    {
        logger.LogInformation("Fetching all mod tags from API.");
        var tags = await client.ModTagAllAsync();
        return tags;
    }

    public async Task<IDictionary<string, ModTag>> GetModTagsDictionary()
    {
        var tags = await GetTags();
        return tags.ToDictionary(t => t.Id, t => t);
    }

    public async Task<Mod?> GetMod(string id)
    {
        logger.LogInformation("Fetching mod {modId} from API.", id);
        try
        {
            return await client.ModGETAsync(id);
        }
        catch (ApiException ex) when (ex.StatusCode == 404)
        {
            logger.LogWarning("Mod {modId} not found in API.", id);
            return null;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error fetching mod {modId} from API.", id);
            throw;
        }
    }

    public async Task<ModTag?> GetModTag(string id)
    {
        logger.LogInformation("Fetching mod tag {tagId} from API.", id);
        try
        {
            return await client.ModTagGETAsync(id);
        }
        catch (ApiException ex) when (ex.StatusCode == 404)
        {
            logger.LogWarning("Mod tag {tagId} not found in API.", id);
            return null;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error fetching mod tag {tagId} from API.", id);
            throw;
        }
    }

    public async Task<ICollection<Mod>> SearchMods(string? search, string[] tags)
    {
        logger.LogInformation("Searching mods with query \"{search}\" and tags: {tags}", search,
            string.Join(",", tags));
        return await client.SearchAsync(search, tags);
    }

    #endregion

    #region Update

    public async Task<Mod> UpdateMod(Mod mod)
    {
        logger.LogInformation("Updating mod {modName}", mod.Name);
        return await client.ModPUTAsync(mod);
    }

    public async Task<ModTag> UpdateModTag(ModTag tag)
    {
        logger.LogInformation("Updating mod tag {tagName}", tag.Name);
        return await client.ModTagPUTAsync(tag);
    }
    
    public async Task<Mod> SetCompromised(string id, bool compromised = true)
    {
        logger.LogInformation("Setting mod {modId} compromised status to {compromised}", id, compromised);
        return await client.CompromisedAsync(id, compromised);
    }

    #endregion

    #region Delete

    public async Task DeleteMod(string id)
    {
        logger.LogInformation("Deleting mod {modId}", id);
        await client.ModDELETEAsync(id);
    }

    public async Task DeleteModTag(string id)
    {
        logger.LogInformation("Deleting mod tag {tagId}", id);
        await client.ModTagDELETEAsync(id);
    }

    #endregion
}