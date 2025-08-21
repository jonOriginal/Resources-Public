using Discord;

namespace Bot;

public static class Util
{
    public static Color ParseToColor(this string? colorString)
    {
        return Color.TryParse(colorString, out var col)
            ? col
            : Color.Default;
    }

    public static ulong ParseToUlong(this string idString)
    {
        return ulong.TryParse(idString, out var id)
            ? id
            : 0;
    }
}