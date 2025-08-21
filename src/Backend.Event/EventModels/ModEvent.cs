namespace Backend.Event.EventModels;

public class ModEvent
{
    public required string ModId { get; set; }

    public required ModEventType EventType { get; set; }
}

public enum ModEventType
{
    Create,
    Update,
    Delete,
    Compromised
}