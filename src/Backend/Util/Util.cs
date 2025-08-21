namespace Backend.Util;

public static class Util
{
    public static string GenerateStringArrayFormat(int length, int startIndex = 0)
    {
        if (length < 1) throw new ArgumentOutOfRangeException(nameof(length), "Length must be at least 1.");
        return string.Join(", ", Enumerable.Range(startIndex, length).Select(i => $"{{{i}}}"));
    }

    public static string GenerateStringArrayFormat<T>(IEnumerable<T> collection, int startIndex = 0)
    {
        return GenerateStringArrayFormat(collection.Count(), startIndex);
    }
}