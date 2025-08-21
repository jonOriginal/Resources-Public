using Backend.Event;
using Backend.Event.EventModels;
using Backend.Event.Services;
using Bot.Services;
using Discord.WebSocket;
using FluentResults;
using StackExchange.Redis;

namespace Bot.Workers;

public class RedisPostEventWorker(
    ILogger<RedisPostEventWorker> logger,
    IConnectionMultiplexer redis,
    ForumService forumService,
    DiscordSocketClient client
)
    : DiscordWorkerBase(client)
{
    private const string ConsumerGroupName = "post-sync-group";

    protected override async Task ExecuteWorkerAsync(CancellationToken stoppingToken)
    {
        var eventClient = new EventClient(redis, logger);
        await eventClient.CreateConsumerGroupIfNotExistsAsync(Streams.PostSync, ConsumerGroupName);

        while (stoppingToken.IsCancellationRequested == false)
        {
            var result = await eventClient.ReadEventsAsync(Streams.PostSync, ConsumerGroupName);
            if (result == null)
            {
                await Task.Delay(TimeSpan.FromSeconds(1), stoppingToken);
                continue;
            }

            await forumService.CreateOrUpdateForumPost(result.Data.ModId);
        }
    }
}