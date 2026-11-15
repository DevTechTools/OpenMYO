namespace OpenMyo.App.Screens.Import;

/// <summary>What a draft track row asks the import screen to do for it.</summary>
public sealed record DraftTrackActions(Action<DraftTrackViewModel> PickIcon, Action<DraftTrackViewModel> Play);
