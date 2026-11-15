namespace OpenMyo.Core.Players;

/// <summary>What to start on a player: a card, the chapter and track to begin at, and how far in.</summary>
public sealed record PlayTarget(string CardId, string ChapterKey, string TrackKey, TimeSpan StartAt);
