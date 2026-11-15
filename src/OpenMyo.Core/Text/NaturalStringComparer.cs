namespace OpenMyo.Core.Text;

/// <summary>Orders strings the way people expect: "Track 2" before "Track 10".</summary>
public sealed class NaturalStringComparer : IComparer<string?>
{
    public static NaturalStringComparer Instance { get; } = new();

    public int Compare(string? x, string? y)
    {
        if (ReferenceEquals(x, y))
        {
            return 0;
        }

        if (x is null || y is null)
        {
            return x is null ? -1 : 1;
        }

        int i = 0, j = 0;
        while (i < x.Length && j < y.Length)
        {
            var comparison = char.IsDigit(x[i]) && char.IsDigit(y[j])
                ? CompareNumberRuns(x, ref i, y, ref j)
                : CompareChars(x[i++], y[j++]);

            if (comparison != 0)
            {
                return comparison;
            }
        }

        return (x.Length - i).CompareTo(y.Length - j);
    }

    private static int CompareChars(char left, char right) =>
        char.ToUpperInvariant(left).CompareTo(char.ToUpperInvariant(right));

    private static int CompareNumberRuns(string x, ref int i, string y, ref int j)
    {
        var left = ReadRun(x, ref i).TrimStart('0');
        var right = ReadRun(y, ref j).TrimStart('0');
        return left.Length != right.Length
            ? left.Length.CompareTo(right.Length)
            : string.CompareOrdinal(left, right);
    }

    private static string ReadRun(string text, ref int index)
    {
        var start = index;
        while (index < text.Length && char.IsDigit(text[index]))
        {
            index++;
        }

        return text[start..index];
    }
}
