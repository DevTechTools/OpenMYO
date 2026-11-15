using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using OpenMyo.Core.Abstractions;
using OpenMyo.Core.Icons;
using OpenMyo.Core.Playlists;

namespace OpenMyo.App.Screens.Library;

/// <summary>
/// Actions that apply to every selected playlist at once. The library hands it the selection and listens for
/// the results; this class never reaches back into the library.
/// </summary>
public sealed partial class BulkActionsViewModel(
    IPlaylistRepository repository, IconAutoAssigner iconAssigner, ILogger<BulkActionsViewModel> logger) : OperationViewModel(logger)
{
    private const int Parallelism = 3;

    /// <summary>First click arms deletion, second click confirms: no modal dialog.</summary>
    [ObservableProperty]
    private bool _isConfirmingDelete;

    /// <summary>The rows the actions apply to; kept current by the library.</summary>
    [ObservableProperty]
    private IReadOnlyList<PlaylistRowViewModel> _selectedRows = [];

    /// <summary>Raised after rows were updated in place, so the library can re-check its filters.</summary>
    public event EventHandler? RowsUpdated;

    /// <summary>Raised with the rows whose playlists were deleted, even when a later delete failed.</summary>
    public event EventHandler<IReadOnlyList<PlaylistRowViewModel>>? RowsDeleted;

    [RelayCommand]
    private Task MatchMissingIconsAsync() => RunAsync("Matching icons", async token =>
    {
        // Rows still loading are skipped; their details are needed to know which icons are missing.
        var rows = SelectedRows.Where(row => row.IsLoaded).ToList();
        var done = 0;

        // Progress reports arrive on the UI thread, which is where rows may change.
        IProgress<(PlaylistRowViewModel Row, Playlist Saved)> applied = new Progress<(PlaylistRowViewModel Row, Playlist Saved)>(result =>
        {
            result.Row.UpdateFrom(result.Saved);
            ReportProgress((double)++done / rows.Count, $"Matched icons for {done} of {rows.Count}");
        });

        var options = new ParallelOptions { MaxDegreeOfParallelism = Parallelism, CancellationToken = token };
        await Parallel.ForEachAsync(rows, options, async (row, rowToken) =>
        {
            if (row.Playlist is not { } original)
            {
                return;
            }

            var matched = (await iconAssigner.MatchIconsAsync(original, replaceExisting: false, useAi: false, rowToken)).Playlist;
            applied.Report((row, ReferenceEquals(matched, original) ? original : await repository.SaveAsync(matched, rowToken)));
        });
        RowsUpdated?.Invoke(this, EventArgs.Empty);
    });

    [RelayCommand]
    private async Task DeleteAsync()
    {
        if (!IsConfirmingDelete)
        {
            IsConfirmingDelete = true;
            return;
        }

        IsConfirmingDelete = false;
        var rows = SelectedRows;
        var deleted = new List<PlaylistRowViewModel>();
        await RunAsync("Deleting", async token =>
        {
            foreach (var row in rows)
            {
                await repository.DeleteAsync(row.Summary.CardId, token);
                deleted.Add(row);
            }
        });

        RowsDeleted?.Invoke(this, deleted);
    }

    [RelayCommand]
    private void CancelDelete() => IsConfirmingDelete = false;
}
