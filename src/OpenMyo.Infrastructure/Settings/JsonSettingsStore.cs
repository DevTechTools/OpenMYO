using System.Text.Json;
using Microsoft.Extensions.Logging;
using OpenMyo.Core.Abstractions;
using OpenMyo.Infrastructure.Paths;

namespace OpenMyo.Infrastructure.Settings;

/// <summary>Stores <see cref="AppSettings"/> as <c>settings.json</c> in the data directory.</summary>
public sealed partial class JsonSettingsStore(IAppPaths paths, ILogger<JsonSettingsStore> logger) : ISettingsStore
{
    private const string FileName = "settings.json";

    public async Task<AppSettings> LoadAsync(CancellationToken cancellationToken)
    {
        var path = Path.Combine(paths.DataDirectory, FileName);
        if (!File.Exists(path))
        {
            return new AppSettings();
        }

        try
        {
            var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, useAsync: true);
            await using (stream.ConfigureAwait(false))
            {
                var settings = await JsonSerializer.DeserializeAsync(
                    stream, SettingsJsonContext.Default.AppSettings, cancellationToken).ConfigureAwait(false);
                return settings ?? new AppSettings();
            }
        }
        catch (Exception ex) when (ex is JsonException or IOException or UnauthorizedAccessException)
        {
            LogUnreadable(ex);
            return new AppSettings();
        }
    }

    public Task SaveAsync(AppSettings settings, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(settings);
        var json = JsonSerializer.SerializeToUtf8Bytes(settings, SettingsJsonContext.Default.AppSettings);
        return AtomicFile.WriteAsync(Path.Combine(paths.DataDirectory, FileName), json, cancellationToken);
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "Settings file could not be read; using defaults.")]
    private partial void LogUnreadable(Exception exception);
}
