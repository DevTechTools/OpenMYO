namespace OpenMyo.Core.Abstractions;

/// <summary>
/// The Claude Desktop app's list of local connectors (MCP servers) on this machine, so OpenMyo can add itself
/// and the user can manage playlists by chatting with Claude on their own subscription.
/// </summary>
public interface IClaudeDesktopConfig
{
    /// <summary>True when Claude Desktop has been installed and run on this machine.</summary>
    bool IsClaudeDesktopInstalled { get; }

    /// <summary>
    /// True while Claude Desktop is open. It rewrites its config from memory as it runs, so a connector added
    /// meanwhile is lost; changes must be made while it is quit.
    /// </summary>
    bool IsClaudeDesktopRunning { get; }

    Task<bool> HasServerAsync(string name, CancellationToken cancellationToken);

    /// <summary>Adds or replaces one connector, keeping every other setting in the file.</summary>
    Task AddServerAsync(string name, string command, IReadOnlyList<string> args, CancellationToken cancellationToken);

    Task RemoveServerAsync(string name, CancellationToken cancellationToken);
}
