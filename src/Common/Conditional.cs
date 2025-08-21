namespace Common;

public class Conditional<T>
{
    private Conditional(T value, bool succeeded)
    {
        Value = value;
        Succeeded = succeeded;
    }

    private T Value { get; }

    public bool Succeeded { get; }

    public static Conditional<T> Of(T value)
    {
        return Continue(value);
    }

    public static Conditional<T> Continue(T value)
    {
        return new Conditional<T>(value, true);
    }

    public static Conditional<T> Stop(T value)
    {
        return new Conditional<T>(value, false);
    }

    public T GetValue()
    {
        return Value;
    }
}

public static class ConditionalExtensions
{
    public static Conditional<T> ToConditional<T>(this T value)
    {
        return Conditional<T>.Of(value);
    }

    public static Conditional<T> ContinueIf<T>(this Conditional<T> conditional, Func<T, bool> predicate)
    {
        if (!conditional.Succeeded)
            return conditional;

        var predicateResult = predicate(conditional.GetValue());
        return predicateResult
            ? Conditional<T>.Continue(conditional.GetValue())
            : Conditional<T>.Stop(conditional.GetValue());
    }

    public static Conditional<T> Apply<T>(this Conditional<T> conditional, Func<T, T> mapFunc)
    {
        if (!conditional.Succeeded)
            return Conditional<T>.Stop(conditional.GetValue());

        var mappedValue = mapFunc(conditional.GetValue());
        return Conditional<T>.Continue(mappedValue);
    }
}