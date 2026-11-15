namespace OpenMyo.Core.Playback;

/// <summary>Where a track sits in a card. Track keys repeat across chapters ("01"), so both keys are needed.</summary>
public readonly record struct TrackAddress(string ChapterKey, string TrackKey);
