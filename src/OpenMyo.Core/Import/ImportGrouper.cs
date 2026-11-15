using System.Text.RegularExpressions;

namespace OpenMyo.Core.Import;

/// <summary>
/// Decides which selected files become which playlist. Every folder that directly contains audio
/// becomes a playlist; "CD 1"/"Disc 2" sub-folders merge into their parent; loose files are
/// grouped by the folder they live in.
/// </summary>
public static partial class ImportGrouper
{
    public static IReadOnlyList<ImportGroup> Group(IEnumerable<string> selectedPaths)
    {
        ArgumentNullException.ThrowIfNull(selectedPaths);

        var byFolder = new Dictionary<string, List<string>>(StringComparer.Ordinal);
        foreach (var file in selectedPaths.SelectMany(ExpandToAudioFiles))
        {
            var folder = PlaylistFolderOf(file);
            if (!byFolder.TryGetValue(folder, out var files))
            {
                byFolder[folder] = files = [];
            }

            files.Add(file);
        }

        return byFolder
            .Select(pair => new ImportGroup(pair.Key, pair.Value.Distinct(StringComparer.Ordinal).ToList()))
            .OrderBy(group => group.Folder, Text.NaturalStringComparer.Instance)
            .ToList();
    }

    /// <summary>True for folder names like "CD1", "Disc 2", "disk_03".</summary>
    public static bool IsDiscFolder(string folderName) => DiscFolder().IsMatch(folderName);

    private static IEnumerable<string> ExpandToAudioFiles(string path)
    {
        if (Directory.Exists(path))
        {
            return Directory.EnumerateFiles(path, "*", SearchOption.AllDirectories)
                .Where(MediaTypes.IsAudio)
                .Where(file => !IsHidden(file));
        }

        return File.Exists(path) && MediaTypes.IsAudio(path) ? [path] : [];
    }

    private static string PlaylistFolderOf(string file)
    {
        var folder = Path.GetDirectoryName(file) ?? string.Empty;
        var parent = Path.GetDirectoryName(folder);
        return parent is not null && IsDiscFolder(Path.GetFileName(folder)) ? parent : folder;
    }

    private static bool IsHidden(string file) => Path.GetFileName(file).StartsWith('.');

    [GeneratedRegex(@"^(cd|dis[ck])[\s_-]*\d{1,2}$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex DiscFolder();
}
