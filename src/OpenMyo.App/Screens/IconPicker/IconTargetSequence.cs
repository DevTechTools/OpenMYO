namespace OpenMyo.App.Screens.IconPicker;

/// <summary>
/// The picker's icon-by-icon flow, shared by every screen that owns a picker: after an icon is chosen, move
/// on to the next target that still has none, so a whole playlist takes one click per track.
/// </summary>
public static class IconTargetSequence
{
    /// <summary>The first target after <paramref name="current"/> without an icon; null when there is none.</summary>
    /// <param name="current">Where to start looking; null starts from the beginning.</param>
    public static T? NextWithoutIcon<T>(IReadOnlyList<T> targets, T? current)
        where T : class, IIconTarget
    {
        ArgumentNullException.ThrowIfNull(targets);

        var start = 0;
        for (var i = 0; current is not null && i < targets.Count; i++)
        {
            if (ReferenceEquals(targets[i], current))
            {
                start = i + 1;
                break;
            }
        }

        return targets.Skip(start).FirstOrDefault(target => !target.HasIcon);
    }
}
