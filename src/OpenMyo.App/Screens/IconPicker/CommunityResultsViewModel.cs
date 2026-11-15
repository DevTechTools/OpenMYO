using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using OpenMyo.App.Services;
using OpenMyo.Core.CommunityIcons;
using OpenMyo.Core.Icons;

namespace OpenMyo.App.Screens.IconPicker;

/// <summary>
/// The picker's community section (yotoicons.com). Searches after typing settles, so stepping through tracks
/// sends a few small requests, not one per keystroke. Choosing an icon uploads it to the user's own icons.
/// </summary>
public sealed partial class CommunityResultsViewModel(
    CommunityIconSearch search, CommunityCatalogProvider catalog, CommunityIconImporter importer, IconBitmapLoader bitmapLoader)
    : ObservableObject, IDisposable
{
    private const int MaxResults = 30;
    private static readonly TimeSpan SettleDelay = TimeSpan.FromMilliseconds(500);

    private CancellationTokenSource? _search;

    [ObservableProperty]
    private bool _isSearching;

    /// <summary>Why there is nothing to show, or what went wrong; null while results are showing.</summary>
    [ObservableProperty]
    private string? _status;

    /// <summary>Raised once a chosen community icon is in the user's account and ready to assign.</summary>
    public event EventHandler<DisplayIcon>? Imported;

    public ObservableCollection<CommunityTileViewModel> Results { get; } = [];

    public async Task SearchAsync(string query)
    {
        _search?.Cancel();
        _search?.Dispose();
        _search = new CancellationTokenSource();
        var token = _search.Token;
        try
        {
            await Task.Delay(SettleDelay, token);
            IsSearching = true;
            await WaitForCatalogAsync(token);
            var icons = string.IsNullOrWhiteSpace(query) ? [] : await search.SearchAsync(query, MaxResults, token);
            token.ThrowIfCancellationRequested();
            ShowMatches(icons);
            Status = icons.Count == 0 ? "No community icons for this title." : null;
        }
        catch (OperationCanceledException)
        {
            return; // A newer search replaced this one.
        }
        catch (HttpRequestException)
        {
            Results.Clear();
            Status = "yotoicons.com isn't reachable right now.";
        }

        IsSearching = false;
    }

    /// <summary>The first run reads every yotoicons.com page once; show how far along that is.</summary>
    private async Task WaitForCatalogAsync(CancellationToken token)
    {
        if (catalog.IsReady)
        {
            return;
        }

        Status = "Getting the yotoicons.com catalog…";
        var progress = new Progress<CommunityHarvestProgress>(report => Status = report.Fraction is { } fraction
            ? $"Getting the yotoicons.com catalog… {fraction:P0}"
            : $"Getting the yotoicons.com catalog… {report.PagesDone} pages");
        await catalog.WarmUpAsync(progress, token);
    }

    public void Dispose()
    {
        _search?.Cancel();
        _search?.Dispose();
        _search = null;
    }

    private void ShowMatches(IReadOnlyList<CommunityIcon> icons)
    {
        Results.Clear();
        foreach (var icon in icons)
        {
            var tile = new CommunityTileViewModel(icon, ChooseAsync);
            Results.Add(tile);
            _ = tile.LoadImageAsync(bitmapLoader); // The loader never throws.
        }
    }

    private async Task ChooseAsync(CommunityTileViewModel tile)
    {
        tile.IsImporting = true;
        try
        {
            var icon = await importer.ImportAsync(tile.Icon, CancellationToken.None);
            Imported?.Invoke(this, icon);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            // UI boundary: the import is optional; say what failed and keep the picker usable.
            Status = "Couldn't add that icon: " + exception.Message;
        }
        finally
        {
            tile.IsImporting = false;
        }
    }
}
