using Backend.Event;
using Backend.Event.EventModels;
using Backend.Event.Services;
using Microsoft.EntityFrameworkCore;

namespace Backend.Posts;

public class PostService(
    EventProducer eventProducer,
    IDbContextFactory<DataContext> ctxFactory
    )
{
    public async Task<List<Post>> GetAll()
    {
        await using var ctx = await ctxFactory.CreateDbContextAsync();
        return await ctx.Posts
            .AsQueryable()
            .OrderBy(forum => forum.Id)
            .ToListAsync();
    }

    public async Task<Post?> GetPost(string postId)
    {
        await using var ctx = await ctxFactory.CreateDbContextAsync();
        return await ctx.Posts
            .AsQueryable()
            .FirstOrDefaultAsync(post => post.Id == postId);
    }

    public async Task<Post?> GetPostByModId(string modId)
    {
        await using var ctx = await ctxFactory.CreateDbContextAsync();
        return await ctx.Posts
            .AsQueryable()
            .FirstOrDefaultAsync(post => post.ModId == modId);
    }

    public async Task CreatePost(Post post)
    {
        await using var ctx = await ctxFactory.CreateDbContextAsync();
        post.CreatedAt = DateTime.UtcNow;
        post.UpdatedAt = DateTime.UtcNow;
        await ctx.Posts.AddAsync(post);
        await ctx.SaveChangesAsync();
    }

    public async Task UpdatePost(Post post)
    {
        await using var ctx = await ctxFactory.CreateDbContextAsync();
        post.UpdatedAt = DateTime.UtcNow;
        ctx.Posts.Update(post);
        await ctx.SaveChangesAsync();
    }

    public async Task DeletePost(string postId)
    {
        await using var ctx = await ctxFactory.CreateDbContextAsync();
        var post = await ctx.Posts.FindAsync(postId);
        if (post == null) return;

        ctx.Posts.Remove(post);
        await ctx.SaveChangesAsync();
    }

    public async Task<PostTag?> GetPostTag(string tagId)
    {
        await using var ctx = await ctxFactory.CreateDbContextAsync();
        return await ctx.PostTags
            .AsQueryable()
            .FirstOrDefaultAsync(tag => tag.Id == tagId);
    }

    public async Task<List<PostTag>> GetAllTags()
    {
        await using var ctx = await ctxFactory.CreateDbContextAsync();
        return await ctx.PostTags
            .AsQueryable()
            .OrderBy(tag => tag.Id)
            .ToListAsync();
    }

    public async Task DeleteTag(string tagId)
    {
        await using var ctx = await ctxFactory.CreateDbContextAsync();
        var tag = await ctx.PostTags.FindAsync(tagId);
        if (tag == null) return;

        ctx.PostTags.Remove(tag);
        await ctx.SaveChangesAsync();
    }
    
    public async Task SyncPost(string modId)
    {
        await eventProducer.PublishAsync(Streams.PostSync, new PostSyncEvent()
        {
            ModId = modId
        });
    }


    public async Task UpdateTag(PostTag postTag)
    {
        await using var ctx = await ctxFactory.CreateDbContextAsync();
        ctx.ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;
        var existing = await ctx.PostTags.FindAsync(postTag.Id);

        postTag.UpdatedAt = DateTime.UtcNow;
        if (existing == null)
        {
            postTag.CreatedAt = DateTime.UtcNow;
            await ctx.PostTags.AddAsync(postTag);
        }
        else
        {
            ctx.PostTags.Update(postTag);
        }

        await ctx.SaveChangesAsync();
    }
}