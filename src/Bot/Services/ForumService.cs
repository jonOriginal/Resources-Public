using System.Text;
using Backend.Api;
using Backend.Api.Services;
using Discord;
using Discord.WebSocket;
using FluentResults;

namespace Bot.Services;

public class ForumService(
    DiscordSocketClient client,
    ModService modService,
    PostService postService,
    IBotConfig config,
    ILogger<ForumService> logger)
{
    private readonly ulong _forumChannelId =
        config.ForumChannelId
        ?? throw new InvalidOperationException("Forum channel ID is not set in the configuration.");

    private readonly ulong _guildId =
        config.GuildId
        ?? throw new InvalidOperationException("Guild ID is not set in the configuration.");

    private static MessageComponent GetMessageComponent(Mod mod)
    {
        var componentBuilder = new ComponentBuilder();
        if (!string.IsNullOrEmpty(mod.WebsiteUrl))
            componentBuilder.WithButton("Website",
                style: ButtonStyle.Link,
                url: mod.WebsiteUrl,
                disabled: mod.IsCompromised
            );

        if (!string.IsNullOrEmpty(mod.DiscordUrl))
            componentBuilder.WithButton("Discord",
                style: ButtonStyle.Link,
                url: mod.DiscordUrl,
                disabled: mod.IsCompromised
            );

        componentBuilder.WithButton(
            "Report Resource",
            "report_resource",
            ButtonStyle.Danger,
            disabled: true);
        return componentBuilder.Build();
    }

    private static string GetPostContent(Mod mod)
    {
        var postContent = new StringBuilder();
        postContent.AppendLine($"# {mod.Name}");
        postContent.AppendLine($"*By: {mod.Author}*");
        if (!string.IsNullOrEmpty(mod.Description)) postContent.AppendLine(mod.Description);

        if (mod.CreatedByUserId != null) postContent.AppendLine($"**Created by:** <@{mod.CreatedByUserId}>");

        if (mod.UpdatedByUserIds is { Count: > 0 })
            postContent.AppendLine(
                $"**Last updated by:** {string.Join(", ", mod.UpdatedByUserIds.Select(id => $"<@{id}>"))}");

        if (mod.CreatedAt.HasValue)
            postContent.AppendLine($"**Created at:** {mod.CreatedAt.Value:yyyy-MM-dd HH:mm:ss}");
        if (mod.UpdatedAt.HasValue)
            postContent.AppendLine($"**Last updated at:** {mod.UpdatedAt.Value:yyyy-MM-dd HH:mm:ss}");

        return postContent.ToString();
    }

    public async Task<Result> CreateForumPost(string id)
    {
        var mod = await modService.GetMod(id);
        if (mod == null)
        {
            logger.LogWarning("Mod with ID {modId} not found", id);
            return Result.Fail($"Mod with ID {id} not found.");
        }

        return await CreateForumPost(mod);
    }

    private async Task<ICollection<ulong>> GetAvailablePostTags(Mod mod)
    {
        var allTags = await postService.GetTags();
        var tagDictionary = allTags.ToDictionary(t => t.ModTagId, t => t);

        if (mod.TagIds == null) return [];

        var tags = new List<ulong>();
        foreach (var tagId in mod.TagIds)
            if (tagDictionary.TryGetValue(tagId, out var tag))
                tags.Add(tag.Id.ParseToUlong());
            else
                logger.LogWarning("Tag with ID {tagId} not found for mod {modId}", tagId, mod.Id);

        return tags;
    }

    private async Task<ICollection<ForumTag>> GetAvailableForumTags(Mod mod, SocketForumChannel channel)
    {
        var availableTags = await GetAvailablePostTags(mod);
        var forumTags = channel.Tags
            .Where(t => availableTags.Contains(t.Id))
            .ToList();
        return forumTags;
    }

    private async Task<FileAttachment?> GetPostAttachment(Mod mod)
    {
        if (string.IsNullOrEmpty(mod.IconUrl)) return null;

        try
        {
            return new FileAttachment(mod.IconUrl);
        }
        catch (Exception e)
        {
            logger.LogError(e, "Failed to download icon for mod {modId}", mod.Id);
            return null;
        }
    }

    public async Task<Result> CreateForumPost(Mod mod)
    {
        var guild = client.GetGuild(_guildId);
        if (guild == null)
        {
            logger.LogCritical("Guild with ID {guildId} not found", _guildId);
            return Result.Fail($"Guild with ID {_guildId} not found.");
        }

        var forumChannel = guild.GetForumChannel(_forumChannelId);
        if (forumChannel == null)
        {
            logger.LogCritical("Forum channel not found for channel {channelId}", _forumChannelId);
            return Result.Fail(
                $"Forum channel with ID {_forumChannelId} not found in guild {client.GetGuild(_guildId).Name}.");
        }

        var availableTags = await GetAvailableForumTags(mod, forumChannel);
        try
        {
            var message = await forumChannel.CreatePostAsync(
                mod.Name,
                ThreadArchiveDuration.OneWeek,
                null,
                GetPostContent(mod),
                tags: availableTags.ToArray(),
                allowedMentions: AllowedMentions.All,
                components: GetMessageComponent(mod)
            );
            await postService.CreatePost(new Post { Id = message.Id.ToString(), ModId = mod.Id });
            return Result.Ok();
        }
        catch (Exception e)
        {
            logger.LogError(e, "Failed to create forum post for mod {modId}", mod.Id);
            return Result.Fail($"Failed to create forum post for mod {mod.Id}: {e.Message}");
        }
    }

    public async Task<Result> DeleteForumPost(ulong postId)
    {
        var forumThread = client.GetGuild(_guildId).GetThreadChannel(postId);
        if (forumThread == null)
        {
            logger.LogWarning("Forum post with ID {postId} not found in guild {guildId}", postId, _guildId);
            return Result.Fail(
                $"Forum post with ID {postId} not found in guild {_guildId}.");
        }

        try
        {
            await forumThread.DeleteAsync();
            await postService.DeletePost(postId.ToString());
            return Result.Ok();
        }
        catch (Exception e)
        {
            logger.LogError(e, "Failed to delete forum thread {threadId}", forumThread.Id);
            return Result.Fail($"Failed to delete forum thread {forumThread.Id}: {e.Message}");
        }
    }
    
    public async Task<Result> CreateOrUpdateForumPost(string id)
    {
        var mod = await modService.GetMod(id);
        if (mod == null)
        {
            logger.LogWarning("Mod with ID {modId} not found", id);
            return Result.Fail($"Mod with ID {id} not found.");
        }

        var post = await postService.GetPostByModId(mod.Id);
        if (post == null)
        {
            return await CreateForumPost(mod);
        }

        return await UpdateForumPost(mod);
    }

    public async Task<Result> UpdateForumPost(Mod mod)
    {
        var guild = client.GetGuild(_guildId);
        if (guild == null)
        {
            logger.LogCritical("Guild with ID {guildId} not found", _guildId);
            return Result.Fail($"Guild with ID {_guildId} not found.");
        }

        var post = await postService.GetPostByModId(mod.Id);
        if (post == null)
        {
            logger.LogWarning("Post with ID {postId} not found for mod {modId}", mod.Id, mod.Id);
            return Result.Fail($"Post with ID {mod.Id} not found for mod {mod.Id}.");
        }

        var forumThread = guild.GetThreadChannel(post.Id.ParseToUlong());
        if (forumThread == null)
        {
            logger.LogWarning("Forum post with ID {postId} not found in guild {guildId}", post.Id, _guildId);
            await postService.DeletePost(post.Id);
            return await CreateForumPost(mod);
        }

        var forumChannel = guild.GetForumChannel(_forumChannelId);
        if (forumChannel == null)
        {
            logger.LogCritical("Forum channel not found for channel {channelId}", _forumChannelId);
            return Result.Fail(
                $"Forum channel with ID {_forumChannelId} not found in guild {client.GetGuild(_guildId).Name}.");
        }

        try
        {
            var availableTags = await GetAvailableForumTags(mod, forumChannel);
            await forumThread.ModifyAsync(
                properties =>
                {
                    properties.Name = mod.Name;
                    properties.AppliedTags = availableTags.Select(t => t.Id).ToArray();
                }
            );
            await forumThread.ModifyMessageAsync(
                forumThread.Id,
                properties =>
                {
                    properties.Embeds = new Optional<Embed[]>();
                    properties.Content = GetPostContent(mod);
                    properties.Components = GetMessageComponent(mod);
                    properties.AllowedMentions = AllowedMentions.All;
                }
            );
            await postService.UpdatePost(new Post
            {
                Id = post.Id,
                ModId = mod.Id
            });
            return Result.Ok();
        }
        catch (Exception e)
        {
            logger.LogError(e, "Failed to update forum post for mod {modId}", mod.Id);
            return Result.Fail($"Failed to update forum post for mod {mod.Id}: {e.Message}");
        }
    }
}