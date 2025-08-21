using Backend.Util;
using Microsoft.EntityFrameworkCore;

namespace Backend.Mods;

public class ModRepo(IDbContextFactory<DataContext> ctxFactory)
{
    public async Task<List<Mod>> Search(string search, string[] tags)
    {
        switch (tags.Length)
        {
            case not 0 when string.IsNullOrEmpty(search):
                return await Search(tags);
            case 0 when string.IsNullOrEmpty(search):
                return await GetAll();
            case 0:
                return await Search(search);
        }

        await using var ctx = await ctxFactory.CreateDbContextAsync();
        var tagsFormatArray = Util.Util.GenerateStringArrayFormat(tags, 1);
        var parameters = ObjectArrayBuilder.From(search).AddRange(tags).Build();

        var query = ctx.Mods.FromSqlRaw(
            $"SELECT * FROM c WHERE (c[\"$type\"] = \"Mod\") AND CONTAINS(c[\"Name\"], {{0}}, true) AND ARRAY_CONTAINS_ALL(c[\"TagIds\"], {tagsFormatArray} )",
            parameters
        );
        return await query.ToListAsync();
    }

    private async Task<List<Mod>> Search(string[] tags)
    {
        await using var ctx = await ctxFactory.CreateDbContextAsync();
        var tagsFormatArray = Util.Util.GenerateStringArrayFormat(tags);
        var parameters = ObjectArrayBuilder.From().AddRange(tags).Build();

        var query = ctx.Mods.FromSqlRaw(
            $"SELECT * FROM c WHERE (c[\"$type\"] = \"Mod\") AND ARRAY_CONTAINS_ALL(c[\"TagIds\"], {tagsFormatArray} )",
            parameters
        );

        return await query.ToListAsync();
    }

    private async Task<List<Mod>> Search(string search)
    {
        await using var ctx = await ctxFactory.CreateDbContextAsync();
        var query = ctx.Mods.FromSqlRaw(
            "SELECT * FROM c WHERE (c[\"$type\"] = \"Mod\") AND CONTAINS(c[\"Name\"], {0}, true)",
            search);

        return await query.ToListAsync();
    }

    public async Task<List<Mod>> SearchByAnyTags(string[] tags)
    {
        await using var ctx = await ctxFactory.CreateDbContextAsync();
        var tagsFormatArray = Util.Util.GenerateStringArrayFormat(tags);
        var parameters = ObjectArrayBuilder.From().AddRange(tags).Build();

        var query = ctx.Mods.FromSqlRaw(
            $"SELECT * FROM c WHERE (c[\"$type\"] = \"Mod\") AND ARRAY_CONTAINS_ANY(c[\"TagIds\"], {tagsFormatArray} )",
            parameters
        );

        return await query.ToListAsync();
    }

    public async Task<List<Mod>> GetAll()
    {
        await using var ctx = await ctxFactory.CreateDbContextAsync();
        return await ctx.Mods
            .AsQueryable()
            .OrderBy(mod => mod.Id)
            .ToListAsync();
    }

    public async Task<Mod?> Get(string id)
    {
        await using var ctx = await ctxFactory.CreateDbContextAsync();
        return await ctx.Mods
            .AsQueryable()
            .FirstOrDefaultAsync(m => m.Id == id);
    }

    public async Task Create(Mod mod)
    {
        await using var ctx = await ctxFactory.CreateDbContextAsync();
        await ctx.Mods.AddAsync(mod);
        await ctx.SaveChangesAsync();
    }

    public async Task Update(Mod mod)
    {
        await using var ctx = await ctxFactory.CreateDbContextAsync();
        ctx.Mods.Update(mod);
        await ctx.SaveChangesAsync();
    }

    public async Task Delete(string id)
    {
        await using var ctx = await ctxFactory.CreateDbContextAsync();
        var mod = await ctx.Mods.FindAsync(id);
        if (mod == null) return;

        ctx.Mods.Remove(mod);
        await ctx.SaveChangesAsync();
    }
}