namespace OpenMyo.Core.Text;

/// <summary>
/// Trigram similarity in the style of PostgreSQL's pg_trgm: each word is padded with two leading
/// spaces and one trailing space and split into overlapping three-character windows.
/// Trigrams are packed into a <see cref="long"/> (three UTF-16 code units) and stored sorted so
/// set intersection is a single linear merge with no allocations.
/// </summary>
public static class Trigrams
{
    /// <summary>Returns the sorted, distinct trigrams of a single normalized word.</summary>
    public static long[] Of(string word)
    {
        ArgumentNullException.ThrowIfNull(word);

        var padded = "  " + word + " ";
        var result = new long[padded.Length - 2];
        for (var i = 0; i < result.Length; i++)
        {
            result[i] = Pack(padded[i], padded[i + 1], padded[i + 2]);
        }

        Array.Sort(result);
        return Distinct(result);
    }

    /// <summary>Jaccard similarity (shared / total distinct) of two sorted trigram sets, from 0 to 1.</summary>
    public static double Similarity(long[] left, long[] right)
    {
        ArgumentNullException.ThrowIfNull(left);
        ArgumentNullException.ThrowIfNull(right);

        if (left.Length == 0 || right.Length == 0)
        {
            return 0;
        }

        var shared = CountShared(left, right);
        return (double)shared / (left.Length + right.Length - shared);
    }

    private static int CountShared(long[] left, long[] right)
    {
        int i = 0, j = 0, shared = 0;
        while (i < left.Length && j < right.Length)
        {
            var comparison = left[i].CompareTo(right[j]);
            if (comparison == 0)
            {
                shared++;
            }

            if (comparison <= 0)
            {
                i++;
            }

            if (comparison >= 0)
            {
                j++;
            }
        }

        return shared;
    }

    private static long Pack(char first, char second, char third) =>
        ((long)first << 32) | ((long)second << 16) | third;

    private static long[] Distinct(long[] sorted)
    {
        var count = 0;
        for (var i = 0; i < sorted.Length; i++)
        {
            if (i == 0 || sorted[i] != sorted[i - 1])
            {
                sorted[count++] = sorted[i];
            }
        }

        return count == sorted.Length ? sorted : sorted[..count];
    }
}
