namespace Backend.Event.EventModels;

public class PostEvent
{
    public required string PostId { get; set; }

    public required PostEventType EventType { get; set; }
}

public enum PostEventType
{
    Create,
    Update,
    Delete,
}