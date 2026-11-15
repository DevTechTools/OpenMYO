using System.ComponentModel;
using System.Diagnostics;
using OpenMyo.Core.Abstractions;
using OpenMyo.Core.ClaudeDesktop;

namespace OpenMyo.App.Services;

/// <summary>
/// Passes AI work to Claude Desktop when OpenMyo is connected to it but has no API key: the request is copied
/// to the clipboard and Claude Desktop is brought forward, so the user only has to paste it. The work then runs
/// on the user's Claude plan through OpenMyo's connector tools.
/// </summary>
public sealed class ClaudeDesktopHandoff(IClaudeDesktopConfig config, WindowProvider window)
{
    // Opens a new chat in Claude Desktop with the text already typed in; the user still presses send.
    private const string NewChatUrl = "claude://claude.ai/new?q=";

    /// <summary>Reads the config now, since Claude Desktop or the user may have changed it since Settings loaded.</summary>
    public async Task<bool> IsConnectedAsync(CancellationToken cancellationToken) =>
        config.IsClaudeDesktopInstalled && await config.HasServerAsync(ConnectorConfigJson.OpenMyoServer, cancellationToken);

    /// <summary>
    /// Opens a new Claude Desktop chat with <paramref name="prompt"/> typed in, and copies it as a fallback.
    /// </summary>
    /// <returns>False when Claude Desktop couldn't be opened; the prompt is still on the clipboard.</returns>
    public async Task<bool> SendAsync(string prompt)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(prompt);

        if (window.TopLevel.Clipboard is { } clipboard)
        {
            await clipboard.SetTextAsync(prompt);
        }

        try
        {
            using var process = Process.Start(new ProcessStartInfo(NewChatUrl + Uri.EscapeDataString(prompt)) { UseShellExecute = true });
            return true;
        }
        catch (Exception exception) when (exception is Win32Exception or InvalidOperationException)
        {
            return false;
        }
    }
}
