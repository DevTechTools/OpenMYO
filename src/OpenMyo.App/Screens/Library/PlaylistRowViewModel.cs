using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using OpenMyo.App.Services;
using OpenMyo.Core.Library;
using OpenMyo.Core.Playlists;

namespace OpenMyo.App.Screens.Library;

/// <summary>A playlist in the library list. Details arrive after the summary.</summary>
public sealed partial class PlaylistRowViewModel : ObservableObject
{
    private const int ShownTrackMatches = 3;

    private readonly IconBitmapLoader _bitmapLoader;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Title), nameof(TitleSegments), nameof(Subtitle), nameof(IsLoaded))]
    private Playlist? _playlist;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasNoTracks), nameof(IsMissingIcons), nameof(IsMissingCover))]
    private PlaylistIssues _issues;

    [ObservableProperty]
    private Bitmap? _cover;

    /// <summary>Why this row matches the current search; null when there is no search or it does not match.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(TitleSegments), nameof(TrackMatches), nameof(HasTrackMatches), nameof(MoreMatchesText),
        nameof(TrackMatchSummary))]
    private PlaylistMatch? _match;

    public PlaylistRowViewModel(PlaylistSummary summary, IconBitmapLoader bitmapLoader)
    {
        Summary = summary;
        _bitmapLoader = bitmapLoader;
        _ = LoadCoverAsync(summary.CoverImageUrl);
    }

    public PlaylistSummary Summary { get; private set; }

    public string Title => Playlist?.Title ?? Summary.Title;

    public string Subtitle => Playlist is null
        ? "Loading…"
        : $"{Playlist.Chapters.Count} tracks · {Math.Round(Playlist.Duration.TotalMinutes)} min";

    public bool IsLoaded => Playlist is not null;

    public bool HasNoTracks => Issues.HasFlag(PlaylistIssues.NoTracks);

    public bool IsMissingIcons => Issues.HasFlag(PlaylistIssues.MissingIcons);

    public bool IsMissingCover => Issues.HasFlag(PlaylistIssues.MissingCover);

    /// <summary>The title, with the searched words highlighted.</summary>
    public IReadOnlyList<TextSegment> TitleSegments => TextSegment.Split(Title, Match?.TitleRanges);

    /// <summary>The first few matching tracks; the rest are counted in <see cref="MoreMatchesText"/>.</summary>
    public IReadOnlyList<TrackMatchViewModel> TrackMatches =>
        Match?.Tracks.Take(ShownTrackMatches).Select(track => new TrackMatchViewModel(this, track)).ToList() ?? [];

    public bool HasTrackMatches => Match is { Tracks.Count: > 0 };

    public string? MoreMatchesText => Match?.Tracks.Count > ShownTrackMatches
        ? $"+{Match.Tracks.Count - ShownTrackMatches} more"
        : null;

    /// <summary>For the grid, where there is no room to list tracks: "♪ 2 matching tracks".</summary>
    public string? TrackMatchSummary => Match?.Tracks.Count switch
    {
        null or 0 => null,
        1 => "♪ 1 matching track",
        var count => $"♪ {count} matching tracks",
    };

    /// <summary>The chapter to show first when this row is opened from a search, or null.</summary>
    public string? FirstMatchKey => Match is { Tracks: [var first, ..] } ? first.ChapterKey : null;

    /// <summary>Re-evaluates <see cref="Match"/>; returns false when the row should be hidden.</summary>
    public bool ApplySearch(SearchQuery query)
    {
        Match = query.IsEmpty ? null : TrackSearch.Match(query, Title, Playlist);
        return query.IsEmpty || Match is not null;
    }

    /// <summary>Applies loaded or saved details and re-evaluates health.</summary>
    public void UpdateFrom(Playlist playlist)
    {
        ArgumentNullException.ThrowIfNull(playlist);

        var coverChanged = playlist.CoverImageUrl != Summary.CoverImageUrl;
        Summary = Summary with { Title = playlist.Title, UpdatedAt = playlist.UpdatedAt ?? Summary.UpdatedAt, CoverImageUrl = playlist.CoverImageUrl };
        Playlist = playlist;
        Issues = PlaylistHealth.Evaluate(playlist);
        if (coverChanged)
        {
            _ = LoadCoverAsync(playlist.CoverImageUrl);
        }
    }

    private async Task LoadCoverAsync(Uri? url) => Cover = await _bitmapLoader.LoadCoverAsync(url);
}
