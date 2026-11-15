using System.Diagnostics;
using OpenMyo.Core.Abstractions;
using OpenMyo.Core.ClaudeDesktop;

namespace OpenMyo.Infrastructure.ClaudeDesktop;

/// <summary>
/// Claude Desktop's claude_desktop_config.json in its per-user folder. Writes go to a temporary file first so
/// an interrupted save never leaves Claude Desktop with a half-written config.
/// </summary>
internal sealed class ClaudeDesktopConfigFile : IClaudeDesktopConfig
{
    private const string FileName = "claude_desktop_config.json";
    private const string ProcessName = "Claude";

    private readonly string _directory = ConfigDirectory();

    public bool IsClaudeDesktopInstalled => Directory.Exists(_directory);

    // "Claude" on both platforms; the lowercase "claude" is Claude Code, which doesn't own this file.
    public bool IsClaudeDesktopRunning
    {
        get
        {
            var processes = Process.GetProcesses();
            try
            {
                return processes.Any(process => string.Equals(process.ProcessName, ProcessName, StringComparison.Ordinal));
            }
            finally
            {
                foreach (var process in processes)
                {
                    process.Dispose();
                }
            }
        }
    }

    private string ConfigPath => Path.Combine(_directory, FileName);

    public async Task<bool> HasServerAsync(string name, CancellationToken cancellationToken) =>
        ConnectorConfigJson.HasServer(await ReadAsync(cancellationToken), name);

    public async Task AddServerAsync(string name, string command, IReadOnlyList<string> args, CancellationToken cancellationToken) =>
        await WriteAsync(ConnectorConfigJson.WithServer(await ReadAsync(cancellationToken), name, command, args), cancellationToken);

    public async Task RemoveServerAsync(string name, CancellationToken cancellationToken) =>
        await WriteAsync(ConnectorConfigJson.WithoutServer(await ReadAsync(cancellationToken), name), cancellationToken);

    private static string ConfigDirectory()
    {
        var root = OperatingSystem.IsMacOS()
            ? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Library", "Application Support")
            : Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        return Path.Combine(root, "Claude");
    }

    private async Task<string?> ReadAsync(CancellationToken cancellationToken) =>
        File.Exists(ConfigPath) ? await File.ReadAllTextAsync(ConfigPath, cancellationToken) : null;

    private async Task WriteAsync(string json, CancellationToken cancellationToken)
    {
        Directory.CreateDirectory(_directory);
        var temporary = ConfigPath + ".openmyo.tmp";
        await File.WriteAllTextAsync(temporary, json, cancellationToken);
        File.Move(temporary, ConfigPath, overwrite: true);
    }
}
