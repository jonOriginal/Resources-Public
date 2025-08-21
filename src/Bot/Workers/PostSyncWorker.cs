using Backend.Api;
using Backend.Api.Services;
using Bot.Services;
using Discord;
using Discord.WebSocket;

namespace Bot.Workers;

public class PostSyncWorker(
    ILogger<PostSyncWorker> logger,
    DiscordSocketClient client,
    ModService modService,
    PostService postService,
    ForumService forumService,
    IBotConfig config
)
    : DiscordWorkerBase(client)
{
    private readonly ulong _forumChannelId =
        config.ForumChannelId
        ?? throw new InvalidOperationException("Forum Channel ID is not set in the configuration.");

    private readonly ulong _guildId =
        config.GuildId
        ?? throw new InvalidOperationException("Guild ID is not set in the configuration.");

    private readonly ulong _notificationChannelId =
        config.NotificationChannelId
        ?? throw new InvalidOperationException("Notification Channel ID is not set in the configuration.");

    protected override async Task ExecuteWorkerAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await SyncMods();
            }
            catch (Exception e)
            {
                logger.LogError(e, "Error during sync loop");
            }

            await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
        }
    }

    private async Task SyncMods()
    {
        logger.LogInformation("Starting mod sync process");
        try
        {
            await SyncForumTags();
            await SyncMissingThreads();
            await CreateMissingThreads();
            await UpdateExistingThreads();
        }
        catch (Exception e)
        {
            logger.LogError(e, "Error during mod sync process");
            await SendNotification(Client, "Mod sync failed", new EmbedBuilder()
                .WithTitle("Mod Sync Error")
                .WithDescription(e.Message)
                .WithColor(Color.Red)
                .Build());
        }

        logger.LogInformation("Mod sync process completed");
    }

    private async Task SyncMissingThreads()
    {
        var guild = Client.GetGuild(_guildId);
        if (guild == null)
        {
            logger.LogCritical("Guild with ID {guildId} not found", _guildId);
            return;
        }

        var threads = guild.ThreadChannels.Select(t => t.Id).ToHashSet();
        logger.LogInformation("Syncing existing posts in forum channel {channelId}", _forumChannelId);
        
        var posts = await postService.GetPosts();
        foreach (var post in posts)
        {
            if (threads.Contains(post.Id.ParseToUlong()))
            {
                logger.LogInformation("Post {postId} already exists, skipping", post.Id);
                continue;
            }

            logger.LogInformation("Post {postId} not found in forum channel, creating", post.Id);
            await postService.DeletePost(post.Id);
        }
    }

    private async Task CreateMissingThreads()
    {
        var mods = await modService.GetModDictionary();
        var posts = (await postService.GetPosts()).ToDictionary(p => p.ModId, p => p);

        foreach (var mod in mods.Values)
        {
            if (posts.TryGetValue(mod.Id, out _)) continue;
            logger.LogInformation("Creating post for mod {modId}", mod.Id);
            await forumService.CreateForumPost(mod.Id);
        }
    }

    private async Task UpdateExistingThreads()
    {
        var mods = await modService.GetModDictionary();
        var posts = (await postService.GetPosts()).ToDictionary(p => p.Id, p => p);

        foreach (var post in posts.Values)
        {
            if (!mods.TryGetValue(post.ModId, out var mod))
            {
                logger.LogWarning("Mod not found for post {postId}, deleting post", post.Id);
                await forumService.DeleteForumPost(post.Id.ParseToUlong());
                continue;
            }

            if (post.UpdatedAt >= mod.UpdatedAt)
            {
                logger.LogInformation("Post {postId} is up-to-date for mod {modId}, skipping", post.Id, mod.Id);
                continue;
            }

            logger.LogInformation("Updating post {postId} for mod {modId}", post.Id, mod.Id);
            await forumService.UpdateForumPost(mod);
        }
    }

    private async Task SendNotification(DiscordSocketClient client, string content, params Embed[] embeds)
    {
        var channel = client.GetGuild(_guildId).GetTextChannel(_notificationChannelId);
        if (channel == null)
        {
            logger.LogCritical("Notification channel not found for channel {channelId}", _notificationChannelId);
            return;
        }

        await channel.SendMessageAsync(content, embeds: embeds);
    }

    private async Task SyncForumTags()
    {
        var forumChannel = GetForumChannel();
        if (forumChannel == null)
        {
            logger.LogCritical("Forum channel not found, aborting tag sync");
            return;
        }

        var namedPostTags = await postService.GetNamedTags();

        await RemoveStaleTagMappings(
            namedPostTags,
            forumChannel.Tags
        );
        await DiscoverNewTags(
            namedPostTags,
            forumChannel.Tags
        );
    }

    private IForumChannel? GetForumChannel()
    {
        var guild = Client.GetGuild(_guildId);
        if (guild == null)
        {
            logger.LogCritical("Guild with ID {guildId} not found", _guildId);
            return null;
        }

        var forumChannel = guild.GetForumChannel(_forumChannelId);
        if (forumChannel == null)
            logger.LogCritical("Forum channel not found for channel {channelId}", _forumChannelId);

        return forumChannel;
    }

    private async Task DiscoverNewTags(ICollection<NamedPostTag> namedTags, IReadOnlyCollection<ForumTag> discordTags)
    {
        var missingNamedTags = namedTags
            .Where(tag => tag.PostTagId == null)
            .ToList();

        var discordTagLookup = discordTags.ToDictionary(
            t => t.Name.Trim(),
            t => t.Id.ToString(),
            StringComparer.OrdinalIgnoreCase
        );

        var addedTags = new List<string>();
        foreach (var namedTag in missingNamedTags)
            if (discordTagLookup.TryGetValue(namedTag.TagName, out var discordTagId))
            {
                logger.LogInformation("Mapping new tag '{tagName}' to Discord tag ID {discordTagId}", namedTag.TagName,
                    discordTagId);
                await postService.UpdateTag(discordTagId, namedTag.ModTagId);
                addedTags.Add(namedTag.TagName);
            }
            else
            {
                logger.LogWarning("No matching Discord tag found for named tag '{tagName}'", namedTag.TagName);
            }

        await UpdatePostsWithTags(addedTags);
    }

    private async Task UpdatePostsWithTags(ICollection<string> tags)
    {
        var modifiablePosts = await modService.GetModsByAnyTags(tags.ToArray());
        if (modifiablePosts.Count > 0)
            foreach (var mod in modifiablePosts)
                await forumService.UpdateForumPost(mod);
        else
            logger.LogInformation("No mods found to update with new tags: {tags}", string.Join(", ", tags));
    }

    private async Task RemoveStaleTagMappings(ICollection<NamedPostTag> namedTags,
        IReadOnlyCollection<ForumTag> discordTags)
    {
        var discordTagIds = discordTags.Select(t => t.Id.ToString()).ToHashSet();

        foreach (var tag in namedTags)
        {
            if (tag.PostTagId == null || discordTagIds.Contains(tag.PostTagId))
                continue;
            logger.LogInformation("Removing stale tag mapping: {tagName}", tag.TagName);
            await postService.DeleteTag(tag.PostTagId);
        }
    }
}