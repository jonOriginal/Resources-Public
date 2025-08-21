namespace Common;

public static class Util
{
    public static string Sanitize(this string? input)
    {
        return string.IsNullOrEmpty(input)
            ? string.Empty
            : input.Trim();
    }

    public static IEnumerable<string> Sanitize(this IEnumerable<string?>? input)
    {
        return input == null
            ? []
            : input
                .Select(x => x!.Trim())
                .Where(x => !string.IsNullOrEmpty(x));
    }
}