using System.Globalization;
using System.Text.RegularExpressions;
using OpenMyo.Core.Text;

namespace OpenMyo.Core.Import;

/// <summary>
/// Puts files in listening order. Uses the most trustworthy signal that every file has:
/// disc + track tags, then a leading number in the file name, then natural file-name order.
/// </summary>
public static partial class TrackOrderer
{
    public static IReadOnlyList<ScannedFile> Order(IReadOnlyList<ScannedFile> files)
    {
        ArgumentNullException.ThrowIfNull(files);

        Func<ScannedFile, int?> number = HasUniqueTagNumbers(files) ? TagNumber
            : files.All(file => FileNameNumber(file) is not null) ? FileNameNumber
            : _ => null;

        return files
            .OrderBy(DiscOf)
            .ThenBy(file => number(file) ?? int.MaxValue)
            .ThenBy(file => System.IO.Path.GetFileName(file.Path), NaturalStringComparer.Instance)
            .ToList();
    }

    private static bool HasUniqueTagNumbers(IReadOnlyList<ScannedFile> files) =>
        files.All(file => file.Metadata.TrackNumber is not null)
        && files.Select(file => (DiscOf(file), file.Metadata.TrackNumber)).Distinct().Count() == files.Count;

    private static int? TagNumber(ScannedFile file) => (int?)file.Metadata.TrackNumber;

    private static int? FileNameNumber(ScannedFile file)
    {
        var match = LeadingNumber().Match(System.IO.Path.GetFileNameWithoutExtension(file.Path));
        return match.Success ? int.Parse(match.Groups["track"].Value, CultureInfo.InvariantCulture) : null;
    }

    /// <summary>Disc from tags, else from a "CD 2" parent folder, else 1.</summary>
    private static int DiscOf(ScannedFile file)
    {
        if (file.Metadata.DiscNumber is { } disc)
        {
            return (int)disc;
        }

        var folder = System.IO.Path.GetFileName(System.IO.Path.GetDirectoryName(file.Path) ?? string.Empty);
        var match = ImportGrouper.IsDiscFolder(folder) ? TrailingNumber().Match(folder) : Match.Empty;
        return match.Success ? int.Parse(match.Value, CultureInfo.InvariantCulture) : 1;
    }

    [GeneratedRegex(@"^\s*(?:(?:cd|dis[ck])?\s*\d{1,2}-)?(?:track\s*)?(?<track>\d{1,3})(?!\d)", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex LeadingNumber();

    [GeneratedRegex(@"\d+$", RegexOptions.CultureInvariant)]
    private static partial Regex TrailingNumber();
}
