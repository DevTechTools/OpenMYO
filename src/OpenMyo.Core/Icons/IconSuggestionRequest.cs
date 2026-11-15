namespace OpenMyo.Core.Icons;

/// <summary>Input for suggesting icons for every track of one playlist.</summary>
/// <param name="PlaylistTitle">Gives context ("Bedtime Stories") and a fallback when a track title matches nothing.</param>
/// <param name="UseAi">The user's explicit choice to let the AI keyword generator (Claude) help. Off by default.</param>
public sealed record IconSuggestionRequest(string PlaylistTitle, IReadOnlyList<string> TrackTitles, bool UseAi = false);
