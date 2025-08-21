using Newtonsoft.Json;
using StackExchange.Redis;

namespace Backend.Event.Services;

public class EventProducer(IConnectionMultiplexer connectionMultiplexer)
{
    public async Task PublishAsync<T>(EventStream<T> stream, T payload)
    {
        var db = connectionMultiplexer.GetDatabase();
        var json = JsonConvert.SerializeObject(payload);
        var entry = new NameValueEntry(Defaults.StreamDataEntryName, json);

        await db.StreamAddAsync(stream.Name, [entry]);
    }
}