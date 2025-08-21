namespace Backend.Util;

public class ObjectArrayBuilder
{
    private readonly List<object?> _objects = [];

    public static ObjectArrayBuilder From(params object?[] objects)
    {
        var builder = new ObjectArrayBuilder();
        foreach (var obj in objects) builder.Add(obj);
        return builder;
    }

    public static ObjectArrayBuilder From(IEnumerable<object?> objects)
    {
        var builder = new ObjectArrayBuilder();
        foreach (var obj in objects) builder.Add(obj);
        return builder;
    }

    public static ObjectArrayBuilder From(object? obj)
    {
        var builder = new ObjectArrayBuilder();
        builder.Add(obj);
        return builder;
    }

    public ObjectArrayBuilder Add(object? obj)
    {
        _objects.Add(obj);
        return this;
    }

    public ObjectArrayBuilder AddRange(IEnumerable<object?> objects)
    {
        _objects.AddRange(objects);
        return this;
    }

    public object?[] Build()
    {
        return _objects.ToArray();
    }
}