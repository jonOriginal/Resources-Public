using Backend.Api;
using Backend.Api.Services;
using Bot.Services;
using Discord;
using Discord.WebSocket;

namespace Bot.Workers;

public class DiscordEventWorker(
    PostService postService,
    ModService modService,
    ForumService forumService,
    IBotConfig config,
    DiscordSocketClient client,
    ILogger<DiscordEventWorker> logger) : DiscordWorkerBase(client)
{
    private readonly ulong _forumChannelId =
        config.ForumChannelId
        ?? throw new InvalidOperationException("Forum channel ID is not set in the configuration.");

    protected override async Task ExecuteWorkerAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Starting Discord event worker");
        Client.ChannelUpdated += HandleChannelUpdated;
        Client.ThreadDeleted += HandleThreadDeleted;

        await Task.Delay(-1, stoppingToken);
    }

    private async Task HandleThreadDeleted(Cacheable<SocketThreadChannel, ulong> cacheableThread)
    {
        var id = cacheableThread.Id;
        logger.LogInformation("Thread {threadId} deleted", id);

        var existingPost = await postService.GetPostById(id.ToString());
        if (existingPost != null)
        {
            logger.LogInformation("Deleting post for thread {threadId}", id);
            await postService.DeletePost(id.ToString());
            await forumService.CreateForumPost(existingPost.ModId);
        }
    }

    private async Task HandleChannelUpdated(SocketChannel oldChannel, SocketChannel updatedChannel)
    {
        if (updatedChannel is not IForumChannel forumChannel)
            return;

        if (oldChannel is not IForumChannel oldForumChannel)
            return;

        if (_forumChannelId != forumChannel.Id)
            return;

        await HandleChannelDeletedTags(oldForumChannel, forumChannel);
        await HandleChannelCreatedTags(oldForumChannel, forumChannel);
    }


    private async Task HandleChannelDeletedTags(IForumChannel oldChannel, IForumChannel updatedChannel)
    {
        logger.LogInformation("Forum channel {channelId} modified", updatedChannel.Id);

        var removedTags = oldChannel.Tags
            .Where(ot => updatedChannel.Tags
                .All(nt => nt.Id != ot.Id)
            )
            .ToList();

        if (removedTags.Count > 0)
            logger.LogInformation("Removed tags: {tags}", string.Join(", ", removedTags.Select(t => t.Name)));

        foreach (var tag in removedTags)
            try
            {
                await postService.DeleteTag(tag.Id.ToString());
                logger.LogInformation("Removed tag {tagName} from forum channel {channelId}", tag.Name,
                    updatedChannel.Id);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to remove tag {tagName} from forum channel {channelId}", tag.Name,
                    updatedChannel.Id);
            }
    }

    private async Task HandleChannelCreatedTags(IForumChannel oldChannel, IForumChannel updatedChannel)
    {
        logger.LogInformation("Forum channel {channelId} modified", updatedChannel.Id);

        var addedTags = updatedChannel.Tags
            .Where(nt => oldChannel.Tags
                .All(ot => ot.Id != nt.Id)
            )
            .ToList();

        if (addedTags.Count > 0)
            logger.LogInformation("Added tags: {tags}", string.Join(", ", addedTags.Select(t => t.Name)));

        var modTags = await modService.GetTags();
        var modTagNameMap = modTags.ToDictionary(mt => mt.Name, StringComparer.OrdinalIgnoreCase);

        var modifiedModTags = new List<ModTag>();
        foreach (var tag in addedTags)
            try
            {
                if (!modTagNameMap.TryGetValue(tag.Name, out var modTag)) continue;

                await postService.UpdateTag(tag.Id.ToString(), modTag.Id);
                modifiedModTags.Add(modTag);

                logger.LogInformation("Updated tag {tagName} in forum channel {channelId}", tag.Name,
                    updatedChannel.Id);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to update tag {tagName} in forum channel {channelId}", tag.Name,
                    updatedChannel.Id);
            }

        var modsToUpdate = await modService.GetModsByAnyTags(modifiedModTags.Select(mt => mt.Id).ToArray());

        foreach (var mod in modsToUpdate)
            try
            {
                await forumService.UpdateForumPost(mod);
                logger.LogInformation("Updated forum post for mod {modId} after tag update", mod.Id);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to update forum post for mod {modId}", mod.Id);
            }
    }
}