using OpenMyo.Core.Playlists;
using OpenMyo.Core.Text;

namespace OpenMyo.Core.Icons;

/// <summary>Fills in chapter icons for a playlist: the best suggestion for each title, or each track's number.</summary>
public sealed class IconAutoAssigner(IIconSuggester suggester, NumberIconMaker numberIcons, CommunityIconFallback community)
{
    /// <summary>
    /// Returns a copy of the playlist with each chapter's best matching icon filled in. Does not save. Icons
    /// come from Yoto's library; a yotoicons.com icon is used only when it matches better than Yoto's best.
    /// </summary>
    /// <param name="replaceExisting">When false, chapters that already have an icon are left alone.</param>
    /// <param name="useAi">The user's choice to let Claude help with titles the trigram matcher is unsure about.</param>
    public async Task<IconMatchResult> MatchIconsAsync(
        Playlist playlist, bool replaceExisting, bool useAi, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(playlist);

        var targets = playlist.Chapters
            .Select((chapter, index) => (chapter, index))
            .Where(pair => replaceExisting || !pair.chapter.HasIcon)
            .ToList();
        if (targets.Count == 0)
        {
            return new IconMatchResult(playlist, 0);
        }

        var series = SeriesWords.Find(playlist.Chapters.Select(chapter => chapter.Title).ToList());
        var queries = targets.Select(pair => IconQuery.FromTitle(pair.chapter.Title, series)).ToList();
        var request = new IconSuggestionRequest(playlist.Title, queries, useAi);
        var suggestions = await suggester.SuggestAsync(request, cancellationToken);

        var toBeat = queries.Select((query, i) => (query, TopScore(suggestions[i]))).ToList();
        var fromCommunity = await community.FindAndImportBetterIconsAsync(toBeat, cancellationToken);

        var chapters = playlist.Chapters.ToArray();
        for (var i = 0; i < targets.Count; i++)
        {
            var icon = fromCommunity.GetValueOrDefault(i) ?? suggestions[i].Best;
            if (icon is not null)
            {
                chapters[targets[i].index] = WithIcon(targets[i].chapter, icon.MediaId);
            }
        }

        return new IconMatchResult(playlist with { Chapters = chapters }, fromCommunity.Count);
    }

    private static double TopScore(IconSuggestion suggestion) =>
        suggestion.Matches.Count > 0 ? suggestion.Matches[0].Score : 0;

    /// <summary>
    /// Gives each chapter at <paramref name="chapterIndexes"/> the icon of its position (track 3 shows "3").
    /// Numbers Yoto lacks are drawn and uploaded; only numbers past 999 are left unchanged and returned.
    /// </summary>
    public async Task<NumberingResult> AssignNumberIconsAsync(Playlist playlist, IReadOnlyList<int> chapterIndexes, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(playlist);
        ArgumentNullException.ThrowIfNull(chapterIndexes);

        var icons = await numberIcons.GetOrCreateNumberIconsAsync(chapterIndexes.Select(index => index + 1), cancellationToken);
        var chapters = playlist.Chapters.ToArray();
        var missing = new List<int>();
        foreach (var chapterIndex in chapterIndexes)
        {
            if (icons.TryGetValue(chapterIndex + 1, out var mediaId))
            {
                chapters[chapterIndex] = WithIcon(chapters[chapterIndex], mediaId);
            }
            else
            {
                missing.Add(chapterIndex + 1);
            }
        }

        return new NumberingResult(playlist with { Chapters = chapters }, missing);
    }

    /// <summary>Sets the icon on a chapter and on each of its tracks, which is what the player displays.</summary>
    public static Chapter WithIcon(Chapter chapter, string? mediaId)
    {
        ArgumentNullException.ThrowIfNull(chapter);

        return chapter with
        {
            IconMediaId = mediaId,
            Tracks = chapter.Tracks.Select(track => track with { IconMediaId = mediaId }).ToList(),
        };
    }
}
