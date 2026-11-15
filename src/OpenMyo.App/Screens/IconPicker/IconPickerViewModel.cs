using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using OpenMyo.App.Screens.Settings;
using OpenMyo.App.Services;
using OpenMyo.Core.Icons;

namespace OpenMyo.App.Screens.IconPicker;

/// <summary>
/// Search-as-you-type icon picker. Opening it for a track pre-fills the search with the track title,
/// so the best matches are already showing: one click assigns. "Search with AI" asks Claude for keywords,
/// only on that explicit click, to keep token use low. Without a key, the picker asks for one in place.
/// </summary>
public sealed partial class IconPickerViewModel : ObservableObject, IDisposable
{
    private const int MaxResults = 60;
    private static readonly TimeSpan TypingDelay = TimeSpan.FromMilliseconds(150);

    private readonly IconIndexProvider _indexProvider;
    private readonly IconBitmapLoader _bitmapLoader;
    private readonly AiIconSearch _aiSearch;
    private readonly ILogger<IconPickerViewModel> _logger;
    private CancellationTokenSource? _search;
    private int? _trackNumber;

    [ObservableProperty]
    private IIconTarget? _target;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SearchWithAiCommand))]
    private string _query = string.Empty;

    /// <summary>Shows the inline API key box after "Search with AI" is clicked without a key.</summary>
    [ObservableProperty]
    private bool _isAskingForKey;

    /// <summary>Shows which keywords the AI chose, so results are explainable.</summary>
    [ObservableProperty]
    private string? _aiKeywords;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SearchWithAiCommand))]
    private bool _isAiSearching;

    public IconPickerViewModel(
        IconIndexProvider indexProvider,
        IconBitmapLoader bitmapLoader,
        AiIconSearch aiSearch,
        AiKeyViewModel aiKey,
        CommunityResultsViewModel community,
        ILogger<IconPickerViewModel> logger)
    {
        (_indexProvider, _bitmapLoader, _aiSearch, _logger) = (indexProvider, bitmapLoader, aiSearch, logger);
        AiKey = aiKey;
        AiKey.Saved += OnAiKeySaved;
        Community = community;
        Community.Imported += OnCommunityImported;
    }

    /// <summary>Icons shared on yotoicons.com, shown below Yoto's own.</summary>
    public CommunityResultsViewModel Community { get; }

    public AiKeyViewModel AiKey { get; }

    /// <summary>Raised after an icon is assigned so the owner can move to the next track.</summary>
    public event EventHandler? IconChosen;

    public ObservableCollection<IconTileViewModel> Results { get; } = [];

    public bool HasTarget => Target is not null;

    /// <param name="trackNumber">The track's position; its number icon is offered first when Yoto has one.</param>
    public void Open(IIconTarget? target, int? trackNumber = null)
    {
        _trackNumber = trackNumber;
        Target = target;
        var query = target?.SearchText ?? string.Empty;
        if (query == Query)
        {
            SearchAsync(query).Forget(_logger, "icon search");
        }

        Query = query;
    }

    /// <summary>True when AI can run now; otherwise shows the inline key box so the user can add one here.</summary>
    public bool CanUseAiOrAskForKey()
    {
        IsAskingForKey = !AiKey.HasKey;
        return AiKey.HasKey;
    }

    partial void OnTargetChanged(IIconTarget? value) => OnPropertyChanged(nameof(HasTarget));

    partial void OnQueryChanged(string value) => SearchAsync(value).Forget(_logger, "icon search");

    [RelayCommand(CanExecute = nameof(CanSearchWithAi))]
    private async Task SearchWithAiAsync()
    {
        if (!CanUseAiOrAskForKey())
        {
            return;
        }

        await CancelSearchAsync();
        IsAiSearching = true;
        try
        {
            var result = await _aiSearch.SearchAsync(Query, CancellationToken.None);
            ShowResults(result.Matches);
            AiKeywords = result.Keywords.Count == 0 ? "AI found no keywords" : "AI searched: " + string.Join(", ", result.Keywords);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            // UI boundary: AI is optional, so a failure is shown inline and the normal results stay usable.
            AiKeywords = "AI search failed: " + exception.Message;
        }
        finally
        {
            IsAiSearching = false;
        }
    }

    /// <summary>
    /// Releases what this picker created. <see cref="Community"/> is not disposed here: the container created it
    /// and disposes it at shutdown, and disposing it twice would hide which object owns it.
    /// </summary>
    public void Dispose()
    {
        AiKey.Saved -= OnAiKeySaved;
        Community.Imported -= OnCommunityImported;
        _search?.Dispose();
    }

    private async Task CancelSearchAsync()
    {
        if (_search is not null)
        {
            await _search.CancelAsync();
            _search.Dispose();
            _search = null;
        }
    }

    private bool CanSearchWithAi() => !IsAiSearching && !string.IsNullOrWhiteSpace(Query);

    // The user was asked for a key because they wanted an AI search: run it as soon as the key is saved.
    private void OnAiKeySaved(object? sender, EventArgs e)
    {
        if (IsAskingForKey)
        {
            IsAskingForKey = false;
            SearchWithAiCommand.Execute(null);
        }
    }

    private async Task SearchAsync(string query)
    {
        await CancelSearchAsync();
        _search = new CancellationTokenSource();
        var token = _search.Token;
        try
        {
            await Task.Delay(TypingDelay, token);
            var index = await _indexProvider.GetAsync(token);
            var matches = string.IsNullOrWhiteSpace(query) ? [] : index.Search(query, MaxResults);
            if (_trackNumber is { } number && index.NumberIcon(number) is { } numberIcon)
            {
                matches = [new IconMatch(numberIcon, 1), .. matches.Where(match => match.Icon != numberIcon)];
            }

            token.ThrowIfCancellationRequested();
            AiKeywords = null;
            ShowResults(matches);
            Community.SearchAsync(query).Forget(_logger, "community icon search");
        }
        catch (OperationCanceledException)
        {
            // A newer search replaced this one.
        }
    }

    private void ShowResults(IReadOnlyList<IconMatch> matches)
    {
        Results.Clear();
        foreach (var match in matches)
        {
            var tile = new IconTileViewModel(match.Icon, ChooseTile);
            Results.Add(tile);
            _ = tile.LoadImageAsync(_bitmapLoader); // The loader never throws.
        }
    }

    private void ChooseTile(IconTileViewModel tile) => AssignToTarget(tile.Icon);

    private void OnCommunityImported(object? sender, DisplayIcon icon) => AssignToTarget(icon);

    private void AssignToTarget(DisplayIcon icon)
    {
        Target?.AssignIcon(icon);
        IconChosen?.Invoke(this, EventArgs.Empty);
    }
}
