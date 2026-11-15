using System.Collections.ObjectModel;
using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OpenMyo.App.Controls;
using OpenMyo.App.Services;
using OpenMyo.Core.Import;

namespace OpenMyo.App.Screens.Import;

/// <summary>A playlist about to be created from a folder of files.</summary>
public sealed partial class DraftCardViewModel : ObservableObject, IReorderableOwner<DraftTrackViewModel>, IReorderableList
{
    private readonly PlaylistDraft _source;
    private readonly Action<DraftCardViewModel> _remove;

    [ObservableProperty]
    private string _title;

    [ObservableProperty]
    private Bitmap? _cover;

    [ObservableProperty]
    private double _progress;

    [ObservableProperty]
    private string? _status;

    public DraftCardViewModel(
        PlaylistDraft source, IconBitmapLoader bitmapLoader, Action<DraftCardViewModel> remove, DraftTrackActions trackActions)
    {
        ArgumentNullException.ThrowIfNull(bitmapLoader);
        (_source, _remove) = (source, remove);
        _title = source.Title;
        foreach (var track in source.Tracks)
        {
            Tracks.Add(new DraftTrackViewModel(track, this, bitmapLoader, trackActions));
        }

        _ = LoadCoverAsync(bitmapLoader);
    }

    public ObservableCollection<DraftTrackViewModel> Tracks { get; } = [];

    public string SourceFolder => _source.SourceLabel ?? _source.SourceFolder;

    public string Summary => $"{Tracks.Count} tracks · {Math.Round(Tracks.Sum(t => t.Duration.TotalMinutes))} min";

    public bool HasCover => _source.Cover is not null;

    public PlaylistDraft ToDraft() => _source with
    {
        Title = string.IsNullOrWhiteSpace(Title) ? _source.Title : Title.Trim(),
        Tracks = Tracks.Select(track => track.ToDraft()).ToList(),
    };

    public void Move(DraftTrackViewModel row, int offset)
    {
        var from = Tracks.IndexOf(row);
        MoveItem(from, from + offset);
    }

    public void MoveItem(int fromIndex, int toIndex)
    {
        var to = Math.Clamp(toIndex, 0, Tracks.Count - 1);
        if (fromIndex >= 0 && fromIndex != to)
        {
            Tracks.Move(fromIndex, to);
        }
    }

    public void Remove(DraftTrackViewModel row)
    {
        Tracks.Remove(row);
        OnPropertyChanged(nameof(Summary));
    }

    [RelayCommand]
    private void Discard() => _remove(this);

    private async Task LoadCoverAsync(IconBitmapLoader bitmapLoader) => Cover = await bitmapLoader.LoadArtworkAsync(_source.Cover);
}
