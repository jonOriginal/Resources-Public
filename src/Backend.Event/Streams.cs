using Backend.Event.EventModels;

namespace Backend.Event;

public static class Streams
{
    public static readonly EventStream<PostEvent> Posts = new("posts");
    
    public static readonly EventStream<PostSyncEvent> PostSync = new("posttags");

    public static readonly EventStream<ModEvent> Mods = new("mods");

    public static readonly EventStream<ModTagEvent> ModTags = new("modtags");
}