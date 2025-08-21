namespace Backend.Event.EventModels;

public class ModTagEvent
{
    public required string ModTagId { get; set; }

    public required ModTagEventType EventType { get; set; }
}

public enum ModTagEventType
{
    Create,
    Update,
    Delete
}