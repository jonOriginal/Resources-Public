using Microsoft.EntityFrameworkCore;

namespace Backend.ModTags;

public class ModTagRepo(IDbContextFactory<DataContext> ctxFactory)
{
    public async Task<List<ModTag>> GetAll()
    {
        await using var ctx = await ctxFactory.CreateDbContextAsync();
        var tags = await ctx.ModTags
            .AsQueryable()
            .OrderBy(tag => tag.Id)
            .ToListAsync();
        return tags;
    }

    public async Task<ModTag?> Get(string id)
    {
        await using var ctx = await ctxFactory.CreateDbContextAsync();
        return await ctx.ModTags
            .AsQueryable()
            .FirstOrDefaultAsync(tag => tag.Id == id);
    }

    public async Task Create(ModTag tag)
    {
        await using var ctx = await ctxFactory.CreateDbContextAsync();
        await ctx.ModTags.AddAsync(tag);
        await ctx.SaveChangesAsync();
    }

    public async Task Update(ModTag tag)
    {
        await using var ctx = await ctxFactory.CreateDbContextAsync();
        ctx.ModTags.Update(tag);
        await ctx.SaveChangesAsync();
    }

    public async Task Delete(string id)
    {
        await using var ctx = await ctxFactory.CreateDbContextAsync();
        var tag = await ctx.ModTags.FindAsync(id);
        if (tag == null) return;

        ctx.ModTags.Remove(tag);
        await ctx.SaveChangesAsync();
    }
}