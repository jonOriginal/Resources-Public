using Backend.Mods;
using Backend.ModTags;
using Backend.Posts;

namespace Backend.NamedPosts;

public class NamedPost
{
    public string? PostId { get; set; }

    public required string ModId { get; set; }

    public required string ModName { get; set; }

    public static NamedPost From(Mod mod, Post post)
    {
        return new NamedPost
        {
            PostId = post.Id,
            ModId = mod.Id,
            ModName = mod.Name
        };
    }

    public static NamedPost From(Mod mod)
    {
        return new NamedPost
        {
            ModId = mod.Id,
            ModName = mod.Name
        };
    }
}

public class NamedPostTag
{
    public string? PostTagId { get; set; }

    public required string ModTagId { get; set; }

    public required string TagName { get; set; }

    public static NamedPostTag From(ModTag modTag)
    {
        return new NamedPostTag
        {
            ModTagId = modTag.Id,
            TagName = modTag.Name
        };
    }

    public static NamedPostTag From(ModTag modTag, PostTag postTag)
    {
        return new NamedPostTag
        {
            PostTagId = postTag.Id,
            ModTagId = modTag.Id,
            TagName = modTag.Name
        };
    }
}