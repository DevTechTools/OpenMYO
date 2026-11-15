namespace OpenMyo.Core.Abstractions;

/// <summary>Persists non-secret user preferences.</summary>
public interface ISettingsStore
{
    Task<AppSettings> LoadAsync(CancellationToken cancellationToken);

    Task SaveAsync(AppSettings settings, CancellationToken cancellationToken);
}

/// <summary>User preferences. Secrets never live here; see <see cref="ISecretStore"/>.</summary>
public sealed record AppSettings
{
    /// <summary>OAuth client id registered at yoto.dev (public PKCE client).</summary>
    public string YotoClientId { get; init; } = string.Empty;

    public string ClaudeModel { get; init; } = "claude-opus-5-5";

    /// <summary>How many audio files upload at once.</summary>
    public int MaxParallelUploads { get; init; } = 4;

    /// <summary>How many podcast episodes download at once when importing from a feed.</summary>
    public int MaxParallelDownloads { get; init; } = 6;

    /// <summary>Shows the library as a grid of covers instead of a list.</summary>
    public bool IsLibraryGrid { get; init; }
}
