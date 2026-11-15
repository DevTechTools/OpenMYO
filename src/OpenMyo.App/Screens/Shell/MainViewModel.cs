using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.Logging;
using OpenMyo.App.Screens.Import;
using OpenMyo.App.Screens.Library;
using OpenMyo.App.Screens.Player;
using OpenMyo.App.Screens.Settings;
using OpenMyo.Core.Abstractions;

namespace OpenMyo.App.Screens.Shell;

/// <summary>
/// Shell: rail navigation, sign-in state and start-up. Links between the pages themselves (an import
/// refreshing the library, and so on) live in <c>Composition/ScreenLinks</c>, not here.
/// </summary>
public sealed partial class MainViewModel : OperationViewModel
{
    private readonly IYotoSession _session;

    private SidebarItem? _selectedItem;

    [ObservableProperty]
    private object? _currentPage;

    public MainViewModel(
        IYotoSession session,
        LibraryViewModel library,
        ImportViewModel import,
        SettingsViewModel settings,
        PlayerBarViewModel playerBar,
        ILogger<MainViewModel> logger)
        : base(logger)
    {
        (_session, Library, ImportPage, SettingsPage, PlayerBar) = (session, library, import, settings, playerBar);
        TopItems = [new("Library", "IconLibrary", SidebarDestination.Library), new("Import", "IconImport", SidebarDestination.Import)];
        BottomItems = [new("Settings", "IconSettings", SidebarDestination.Settings)];

        session.SignInStateChanged += (_, _) => Avalonia.Threading.Dispatcher.UIThread.Post(OnSignInStateChanged);
        SelectedItem = TopItems[0];
    }

    /// <summary>The active destination. Exposed to the view as two list selections (top and bottom of the rail).</summary>
    public SidebarItem? SelectedItem
    {
        get => _selectedItem;
        set
        {
            if (value is null || !SetProperty(ref _selectedItem, value))
            {
                return;
            }

            OnPropertyChanged(nameof(SelectedTopItem));
            OnPropertyChanged(nameof(SelectedBottomItem));
            Navigate(value);
        }
    }

    public SidebarItem? SelectedTopItem
    {
        get => _selectedItem is not null && TopItems.Contains(_selectedItem) ? _selectedItem : null;
        set => SelectedItem = value;
    }

    public SidebarItem? SelectedBottomItem
    {
        get => _selectedItem is not null && BottomItems.Contains(_selectedItem) ? _selectedItem : null;
        set => SelectedItem = value;
    }

    public LibraryViewModel Library { get; }

    public ImportViewModel ImportPage { get; }

    public SettingsViewModel SettingsPage { get; }

    /// <summary>The player bar under every page.</summary>
    /// <summary>Now playing, here or on a Yoto player; docked under every page.</summary>
    public PlayerBarViewModel PlayerBar { get; }

    /// <summary>Pages at the top of the rail.</summary>
    public IReadOnlyList<SidebarItem> TopItems { get; }

    /// <summary>Pages pinned to the bottom of the rail.</summary>
    public IReadOnlyList<SidebarItem> BottomItems { get; }

    public bool IsSignedIn => _session.IsSignedIn;

    /// <summary>The library needs an account; other pages work signed out.</summary>
    public bool ShowSignInPrompt => !IsSignedIn && CurrentPage is LibraryViewModel;

    /// <summary>
    /// Restores the previous session silently. A restored session raises the sign-in event, which opens and
    /// loads the library; without one, Settings opens so the user can sign in.
    /// </summary>
    public Task InitializeAsync() => RunAsync("Connecting to Yoto", async token =>
    {
        await SettingsPage.LoadAsync(token);
        await Library.LoadLayoutAsync(token);
        if (!await _session.TryRestoreAsync(token))
        {
            SelectedItem = BottomItems[0];
        }
    });

    private void Navigate(SidebarItem value)
    {
        CurrentPage = value.Destination switch
        {
            SidebarDestination.Import => ImportPage,
            SidebarDestination.Settings => SettingsPage,
            _ => Library,
        };

        OnPropertyChanged(nameof(ShowSignInPrompt));
    }

    private void OnSignInStateChanged()
    {
        OnPropertyChanged(nameof(IsSignedIn));
        OnPropertyChanged(nameof(ShowSignInPrompt));
        if (_session.IsSignedIn)
        {
            SelectedItem = TopItems[0];
        }
    }
}
