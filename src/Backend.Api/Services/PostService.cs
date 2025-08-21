namespace Backend.Api.Services;

public class PostService(BackendClient client)
{
    public async Task<ICollection<Post>> GetPosts()
    {
        return await client.ListAsync();
    }

    public async Task<Post?> GetPostByModId(string modId)
    {
        try
        {
            return await client.ModAsync(modId);
        }
        catch (ApiException e) when (e.StatusCode == 404)
        {
            return null;
        }
    }
    
    public async Task<Post?> GetPostById(string postId)
    {
        try
        {
            return await client.PostGETAsync(postId);
        }
        catch (ApiException e) when (e.StatusCode == 404)
        {
            return null;
        }
    }

    public async Task UpdatePost(Post post)
    {
        await client.PostPUTAsync(post);
    }

    public async Task Sync(string modId)
    {
        await client.SyncAsync(modId);
    }

    public async Task CreatePost(Post post)
    {
        await client.PostPOSTAsync(post);
    }

    public async Task<ICollection<PostTag>> GetTags()
    {
        return await client.TagsAsync();
    }

    public async Task DeletePost(string postId)
    {
        try
        {
            await client.PostDELETEAsync(postId);
        }
        catch (ApiException e) when (e.StatusCode == 404)
        {
        }
    }

    public async Task<ICollection<NamedPost>> GetNamedPosts()
    {
        return await client.List2Async();
    }

    public async Task<ICollection<NamedPostTag>> GetNamedTags()
    {
        return await client.Tags2Async();
    }

    public async Task DeleteTag(string postTagId)
    {
        await client.TagDELETEAsync(postTagId);
    }

    public async Task UpdateTag(string postTagId, string modTagId)
    {
        var tag = new PostTag
        {
            Id = postTagId,
            ModTagId = modTagId
        };
        await client.TagPUTAsync(tag);
    }
}