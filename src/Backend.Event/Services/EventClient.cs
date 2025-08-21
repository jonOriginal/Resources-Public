using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using StackExchange.Redis;

namespace Backend.Event.Services;

public class EventClient(IConnectionMultiplexer connectionMultiplexer, ILogger logger)
{
    private readonly string _consumerName = $"consumer-{Guid.NewGuid()}";

    private readonly IDatabase _db = connectionMultiplexer.GetDatabase();

    public async Task<EventResult<T>?> ReadEventsAsync<T>(EventStream<T> stream, string groupName)
    {
        var entries = await _db.StreamReadGroupAsync(
            stream.Name,
            groupName,
            _consumerName,
            count: 1,
            position: StreamPosition.NewMessages
        );

        if (entries.Length == 0)
        {
            logger.LogDebug("No new events found in stream {StreamName} for group {GroupName}", stream.Name,
                groupName);
            return null;
        }

        var entry = entries.First();

        var json = entry.Values.FirstOrDefault(v => v.Name == Defaults.StreamDataEntryName);
        if (!json.Value.HasValue)
        {
            logger.LogWarning("No data found in stream entry {EntryId} for stream {StreamName} and group {GroupName}",
                entry.Id, stream.Name, groupName);
            return null;
        }

        try
        {
            var payload = JsonConvert.DeserializeObject<T>(json.Value);
            if (payload != null)
                return new EventResult<T> { MessageId = entry.Id, Data = payload, ConsumerGroup = groupName };
        }
        catch (JsonException)
        {
            logger.LogError("Failed to deserialize event from stream {StreamName} for group {GroupName}: {Json}",
                stream.Name, groupName, json);
        }
        catch (Exception ex)
        {
            logger.LogError(ex,
                "Unexpected error while processing event from stream {StreamName} for group {GroupName}",
                stream.Name, groupName);
        }

        return null;
    }

    public async Task AcknowledgeEventsAsync<T>(EventStream<T> stream, EventResult<T> result)
    {
        await _db.StreamAcknowledgeAsync(new RedisKey(stream.Name), result.ConsumerGroup, result.MessageId);
    }

    public async Task CreateConsumerGroupIfNotExistsAsync<T>(EventStream<T> stream, string groupName)
    {
        try
        {
            await _db.StreamCreateConsumerGroupAsync(stream.Name, groupName, StreamPosition.NewMessages);
            logger.LogInformation("Created Redis consumer group '{Group}' on stream '{Stream}'", groupName,
                stream.Name);
        }
        catch (RedisServerException ex) when (ex.Message.Contains("BUSYGROUP"))
        {
            logger.LogInformation("Consumer group '{Group}' already exists on stream '{Stream}'", groupName,
                stream.Name);
        }
    }
}