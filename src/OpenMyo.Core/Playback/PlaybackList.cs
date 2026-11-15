namespace OpenMyo.Core.Playback;

/// <summary>An ordered list of items being played, and which one is current.</summary>
public sealed class PlaybackList
{
    /// <summary>"Previous" within this time of a track's start goes back a track; later it restarts the track.</summary>
    public static readonly TimeSpan RestartThreshold = TimeSpan.FromSeconds(3);

    private int _index;

    public PlaybackList(IReadOnlyList<PlaybackItem> items, int startIndex)
    {
        ArgumentNullException.ThrowIfNull(items);
        ArgumentOutOfRangeException.ThrowIfZero(items.Count);
        ArgumentOutOfRangeException.ThrowIfNegative(startIndex);
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(startIndex, items.Count);
        (Items, _index) = (items, startIndex);
    }

    public IReadOnlyList<PlaybackItem> Items { get; }

    public PlaybackItem Current => Items[_index];

    public int CurrentIndex => _index;

    public bool HasNext => _index < Items.Count - 1;

    /// <summary>Moves to the next item. Returns false, and stays put, at the end of the list.</summary>
    public bool MoveNext()
    {
        if (!HasNext)
        {
            return false;
        }

        _index++;
        return true;
    }

    /// <summary>
    /// Like a music player: goes back a track near the start of the current one, otherwise the caller restarts it.
    /// Returns true when the current item changed.
    /// </summary>
    public bool MovePrevious(TimeSpan position)
    {
        if (_index == 0 || position > RestartThreshold)
        {
            return false;
        }

        _index--;
        return true;
    }
}
