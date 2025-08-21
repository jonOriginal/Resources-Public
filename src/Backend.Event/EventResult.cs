namespace Backend.Event;

public class EventResult<T>
{
    public required string MessageId { get; set; }

    public required string ConsumerGroup { get; set; }

    public required T Data { get; set; }
}