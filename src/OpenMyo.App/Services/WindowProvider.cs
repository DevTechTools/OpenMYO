using Avalonia.Controls;

namespace OpenMyo.App.Services;

/// <summary>Gives services access to the main window's top level (for pickers) without view models seeing it.</summary>
public sealed class WindowProvider
{
    private TopLevel? _topLevel;

    public TopLevel TopLevel => _topLevel ?? throw new InvalidOperationException("The main window has not been created yet.");

    public void Attach(TopLevel topLevel) => _topLevel = topLevel;
}
