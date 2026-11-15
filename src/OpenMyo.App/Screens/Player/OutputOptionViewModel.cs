using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OpenMyo.Core.Players;

namespace OpenMyo.App.Screens.Player;

/// <summary>One choice in the output menu: this computer (no player) or a Yoto player.</summary>
public sealed partial class OutputOptionViewModel(YotoPlayer? player, Action<OutputOptionViewModel> choose) : ObservableObject
{
    public const string ThisComputer = "This computer";

    [ObservableProperty]
    private bool _isSelected;

    /// <summary>The Yoto player, or null for this computer.</summary>
    public YotoPlayer? Player { get; } = player;

    public string Name => Player?.Name ?? ThisComputer;

    /// <summary>Offline players stay listed, dimmed, so people can see they exist but cannot be used now.</summary>
    public bool IsAvailable => Player is null or { IsOnline: true };

    /// <summary>Menu text: the name, plus "Offline" when it cannot be chosen now.</summary>
    public string Label => IsAvailable ? Name : $"{Name} (offline)";

    [RelayCommand]
    private void Choose() => choose(this);

    public string AccessibleName => IsAvailable ? $"Play on {Name}" : $"{Name}, offline";
}
