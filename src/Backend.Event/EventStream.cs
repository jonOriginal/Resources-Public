namespace Backend.Event;

public record EventStream<T>(string Name)
{
    public Type EventDataType => typeof(T);
}