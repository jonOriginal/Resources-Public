using Backend.Mods;
using Backend.ModTags;
using Backend.Posts;

namespace Backend.NamedPosts;

public class NamedPostService(
    ModService service,
    PostService postService,
    ModTagService modTagService)
{
    public async Task<List<NamedPostTag>> GetAllTags()
    {
        var modTags = await modTagService.GetAll();
        var postTags = await postService.GetAllTags();

        var postTagLookup = postTags.ToDictionary(pt => pt.ModTagId, pt => pt);

        return modTags.Select(modTag => postTagLookup.TryGetValue(modTag.Id, out var postTag)
                ? NamedPostTag.From(modTag, postTag)
                : NamedPostTag.From(modTag))
            .ToList();
    }

    public async Task<List<NamedPost>> GetAll()
    {
        var mods = await service.GetAll();
        var posts = await postService.GetAll();

        var postLookup = posts.ToDictionary(p => p.ModId, p => p);

        return mods.Select(mod => postLookup.TryGetValue(mod.Id, out var post)
                ? NamedPost.From(mod, post)
                : NamedPost.From(mod))
            .ToList();
    }
}