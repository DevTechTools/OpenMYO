using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using OpenMyo.Core.Abstractions;
using OpenMyo.Core.Players;

namespace OpenMyo.App.Screens.Player;

/// <summary>
/// Where playback goes: this computer or one of the user's Yoto players. The list refreshes each time the menu
/// opens. The choice is deliberately not saved, so starting the app never plays in another room unexpectedly.
/// </summary>
public sealed partial class OutputPickerViewModel : OperationViewModel
{
    private readonly IPlayerDirectory _directory;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsRemote), nameof(OutputName), nameof(ButtonLabel))]
    private YotoPlayer? _selectedPlayer;

    /// <summary>Raised when the user picks another output; the bar moves playback there.</summary>
    public event EventHandler<YotoPlayer?>? OutputChosen;

    public OutputPickerViewModel(IPlayerDirectory directory, ILogger<OutputPickerViewModel> logger)
        : base(logger)
    {
        _directory = directory;
        Options.Add(new OutputOptionViewModel(null, Choose) { IsSelected = true });
    }

    public ObservableCollection<OutputOptionViewModel> Options { get; } = [];

    public bool IsRemote => SelectedPlayer is not null;

    public string OutputName => SelectedPlayer?.Name ?? OutputOptionViewModel.ThisComputer;

    public string ButtonLabel => $"Playing on {OutputName}. Choose where to play";

    /// <summary>Lists the players again; called as the menu opens so online states are current.</summary>
    [RelayCommand]
    private Task RefreshAsync() => RunAsync("Looking for Yoto players", async token =>
    {
        var players = await _directory.ListAsync(token);
        Options.Clear();
        Options.Add(new OutputOptionViewModel(null, Choose));
        foreach (var player in players)
        {
            Options.Add(new OutputOptionViewModel(player, Choose));
        }

        MarkSelected();
    });

    private void Choose(OutputOptionViewModel option)
    {
        if (!option.IsAvailable || option.Player?.DeviceId == SelectedPlayer?.DeviceId)
        {
            return;
        }

        OutputChosen?.Invoke(this, option.Player);
    }

    /// <summary>Records where playback now goes, after the bar has moved it (or fell back to this computer).</summary>
    public void SetOutput(YotoPlayer? player)
    {
        SelectedPlayer = player;
        MarkSelected();
    }

    private void MarkSelected()
    {
        foreach (var option in Options)
        {
            option.IsSelected = option.Player?.DeviceId == SelectedPlayer?.DeviceId;
        }
    }
}
