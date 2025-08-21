using System.Text;
using Backend.Api.Services;
using Backend.Event;
using Backend.Event.EventModels;
using Backend.Event.Services;
using Bot.Services;
using Discord;
using Discord.WebSocket;
using FluentResults;
using StackExchange.Redis;

namespace Bot.Workers;

public class RedisModEventWorker(
    IBotConfig config,
    ILogger<RedisModEventWorker> logger,
    IConnectionMultiplexer redis,
    ForumService forumService,
    ModService modService,
    DiscordSocketClient client
)
    : DiscordWorkerBase(client)
{
    private const string ConsumerGroupName = "mod-sync-group";

    protected override async Task ExecuteWorkerAsync(CancellationToken stoppingToken)
    {
        var eventClient = new EventClient(redis, logger);
        await eventClient.CreateConsumerGroupIfNotExistsAsync(Streams.Mods, ConsumerGroupName);

        while (stoppingToken.IsCancellationRequested == false)
        {
            var result = await eventClient.ReadEventsAsync(Streams.Mods, ConsumerGroupName);
            if (result == null)
            {
                await Task.Delay(TimeSpan.FromSeconds(1), stoppingToken);
                continue;
            }

            switch (result.Data.EventType)
            {
                case ModEventType.Create:
                {
                    var createResult = await HandleCreate(result.Data.ModId);
                    if (createResult.IsSuccess) await eventClient.AcknowledgeEventsAsync(Streams.Mods, result);
                    break;
                }
                case ModEventType.Update:
                    var updateResult = await HandleUpdate(result.Data.ModId);
                    if (updateResult.IsSuccess) await eventClient.AcknowledgeEventsAsync(Streams.Mods, result);
                    break;
                case ModEventType.Delete:
                    var deleteResult = await HandleDelete(result.Data.ModId);
                    if (deleteResult.IsSuccess) await eventClient.AcknowledgeEventsAsync(Streams.Mods, result);
                    break;
                case ModEventType.Compromised:
                    var compromiseResult = await HandleCompromiseNotification(result.Data.ModId);
                    if (compromiseResult.IsSuccess)
                        await eventClient.AcknowledgeEventsAsync(Streams.Mods, result);
                    break;
                default:
                    logger.LogWarning("Unknown mod event type: {EventType}", result.Data.EventType);
                    continue;
            }
        }
    }
    
    private async Task<Result> HandleCompromiseNotification(string modId)
    {
        var mod = await modService.GetMod(modId);
        if (mod == null)
        {
            return Result.Fail($"Mod with ID {modId} not found");
        }
        
        if (config.UpdateChannelId == null)
        {
            return Result.Fail("Update channel ID is not configured");
        }

        if (Client.GetChannel(config.UpdateChannelId.Value) is not ISocketMessageChannel channel)
        {
            return Result.Fail("Update channel not found");
        }
        
        var message = new StringBuilder();
        message.AppendLine($"# Resource Compromised Notification");
        message.AppendLine();
        message.AppendLine($"## *{mod.Name}* has been marked as compromised.");
        message.AppendLine($"If you are using this mod or resource, please remove it immediately and secure your account.");
        message.AppendLine();
        message.AppendLine($"@everyone");
        
        await channel.SendMessageAsync(message.ToString(), allowedMentions: AllowedMentions.All);
        return Result.Ok();
    }

    private async Task<Result> HandleCreate(string id)
    {
        var mod = await modService.GetMod(id);
        if (mod == null)
        {
            logger.LogWarning("Mod with ID {ModId} not found for creation", id);
            return Result.Fail($"Mod with ID {id} not found");
        }

        return await forumService.CreateForumPost(mod);
    }

    private async Task<Result> HandleUpdate(string id)
    {
        var mod = await modService.GetMod(id);
        if (mod == null)
        {
            logger.LogWarning("Mod with ID {ModId} not found for update", id);
            return Result.Fail($"Mod with ID {id} not found");
        }

        return await forumService.UpdateForumPost(mod);
    }

    private async Task<Result> HandleDelete(string id)
    {
        var mod = await modService.GetMod(id);
        if (mod == null)
        {
            logger.LogWarning("Mod with ID {ModId} not found for deletion", id);
            return Result.Fail($"Mod with ID {id} not found");
        }

        var deleteResult = await forumService.DeleteForumPost(mod.Id.ParseToUlong());
        if (deleteResult.IsFailed)
        {
            logger.LogError("Failed to delete forum post for mod {ModId}: {Errors}", mod.Id, deleteResult.Errors);
            return Result.Fail(deleteResult.Errors);
        }

        return Result.Ok();
    }
}