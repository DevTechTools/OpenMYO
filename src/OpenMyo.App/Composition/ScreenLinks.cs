using Avalonia.Threading;
using Microsoft.Extensions.Logging;
using OpenMyo.App.Screens.Import;
using OpenMyo.App.Screens.Library;
using OpenMyo.App.Screens.Player;
using OpenMyo.App.Screens.Settings;
using OpenMyo.App.Services;
using OpenMyo.Core.Abstractions;
using OpenMyo.Core.Icons;

namespace OpenMyo.App.Composition;

/// <summary>
/// Every link between screens, in one place. Screens only raise events about themselves ("playlists created",
/// "library loaded"); this class decides who reacts, so no page's view model needs to know another page's.
/// The shell's own reactions (navigation, the sign-in prompt) stay in <c>MainViewModel</c>.
/// </summary>
internal sealed class ScreenLinks(
    IYotoSession session,
    LibraryViewModel library,
    ImportViewModel import,
    SettingsViewModel settings,
    PlayerBarViewModel playerBar,
    IconIndexProvider iconIndex,
    ILogger<ScreenLinks> logger)
{
    /// <summary>Subscribes once, at start-up; the screens are singletons that live as long as the app.</summary>
    public void Connect()
    {
        // The session raises this from whichever thread signed in or out.
        session.SignInStateChanged += (_, _) => Dispatcher.UIThread.Post(OnSignInStateChanged);

        // New playlists belong in the library straight away.
        import.PlaylistsCreated += (_, _) => library.RefreshAsync().Forget(logger, "refreshing the library after an import");

        // Icons already used in the library become searchable in every icon picker.
        library.PlaylistsLoaded += (_, playlists) => iconIndex.SetLibraryIcons(LibraryIcons.FromPlaylists(playlists));

        // Play buttons on rows go to the player bar, which plays here or on the chosen Yoto player.
        library.Editor.PlayRequested += (_, request) => playerBar.Play(request);
        import.PlayRequested += (_, request) => playerBar.Play(request);
        playerBar.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(PlayerBarViewModel.CurrentRowKey))
            {
                library.Editor.ShowPlaying(playerBar.CurrentRowKey);
                import.ShowPlaying(playerBar.CurrentRowKey);
            }
        };
        library.Editor.Chapters.CollectionChanged += (_, _) => library.Editor.ShowPlaying(playerBar.CurrentRowKey);
        import.Drafts.CollectionChanged += (_, _) => import.ShowPlaying(playerBar.CurrentRowKey);
    }

    private void OnSignInStateChanged()
    {
        settings.RefreshSignInState();
        if (!session.IsSignedIn)
        {
            playerBar.StopAllAsync().Forget(logger, "stopping playback after sign-out");
        }

        if (session.IsSignedIn)
        {
            library.RefreshAsync().Forget(logger, "loading the library after sign-in");
            playerBar.WatchPlayersAsync().Forget(logger, "watching Yoto players after sign-in");
        }
    }
}
