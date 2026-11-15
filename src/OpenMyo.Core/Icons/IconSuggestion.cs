namespace OpenMyo.Core.Icons;

/// <summary>Ranked icon candidates for one track. <see cref="Best"/> is null when nothing matched.</summary>
public sealed record IconSuggestion(int TrackIndex, IReadOnlyList<IconMatch> Matches)
{
    public DisplayIcon? Best => Matches.Count > 0 ? Matches[0].Icon : null;
}
