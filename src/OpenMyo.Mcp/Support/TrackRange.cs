using ModelContextProtocol;
using OpenMyo.Core.Playlists;

namespace OpenMyo.Mcp.Support;

/// <summary>Checks the 1-based track numbers Claude sends against a playlist.</summary>
public static class TrackRange
{
    /// <summary>Throws an <see cref="McpException"/> naming every track that is not in the playlist.</summary>
    public static void Validate(Playlist playlist, IEnumerable<int> tracks)
    {
        var bad = tracks.Where(track => track < 1 || track > playlist.Chapters.Count).Distinct().ToList();
        if (bad.Count > 0)
        {
            throw new McpException(
                $"Track(s) {string.Join(", ", bad)} are out of range; '{playlist.Title}' has {playlist.Chapters.Count} tracks (1-based).");
        }
    }
}
