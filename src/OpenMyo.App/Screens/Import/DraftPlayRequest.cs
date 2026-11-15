using OpenMyo.Core.Import;

namespace OpenMyo.App.Screens.Import;

/// <summary>Preview <see cref="Draft"/>'s files from the one at <see cref="TrackIndex"/>.</summary>
public sealed record DraftPlayRequest(PlaylistDraft Draft, int TrackIndex);
