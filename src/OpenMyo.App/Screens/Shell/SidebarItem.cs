namespace OpenMyo.App.Screens.Shell;

/// <summary>Where a rail entry navigates.</summary>
public enum SidebarDestination
{
    Library,
    Import,
    Settings,
}

/// <summary>An entry in the navigation rail: an icon, with its title as the tooltip.</summary>
public sealed class SidebarItem(string title, string iconKey, SidebarDestination destination)
{
    public string Title { get; } = title;

    /// <summary>Key of a <c>StreamGeometry</c> resource in Styles/Theme.axaml.</summary>
    public string IconKey { get; } = iconKey;

    public SidebarDestination Destination { get; } = destination;
}
