namespace OpenMyo.Core.Feeds;

/// <summary>Track order for a playlist made from a feed.</summary>
public enum EpisodeOrder
{
    /// <summary>Episode 1 first: suits story series and audiobooks.</summary>
    OldestFirst,

    /// <summary>The latest episode first, as podcast apps list them.</summary>
    NewestFirst,
}
