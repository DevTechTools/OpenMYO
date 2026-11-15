using Microsoft.Extensions.DependencyInjection;
using OpenMyo.Core.Abstractions;
using OpenMyo.Infrastructure.Audio;
using OpenMyo.Infrastructure.ClaudeDesktop;
using OpenMyo.Infrastructure.Feeds;
using OpenMyo.Infrastructure.Paths;
using OpenMyo.Infrastructure.Secrets;
using OpenMyo.Infrastructure.Settings;

namespace OpenMyo.Infrastructure;

public static class ServiceCollectionExtensions
{
    /// <summary>Registers local-machine services. Requires <c>ILogger&lt;T&gt;</c> (AddLogging) to be available.</summary>
    public static IServiceCollection AddInfrastructure(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddSingleton<IAppPaths, AppPaths>();
        services.AddSingleton<ISettingsStore, JsonSettingsStore>();
        services.AddSingleton<IAudioMetadataReader, TagLibAudioMetadataReader>();
        services.AddSingleton<IAudioPlayer, VlcAudioPlayer>();
        services.AddSingleton<IClaudeDesktopConfig, ClaudeDesktopConfigFile>();
        services.AddHttpClient(HttpFeedClient.HttpClientName, client =>
        {
            // Episodes can be hours long; the feed request has its own timeout and callers cancel downloads.
            client.Timeout = Timeout.InfiniteTimeSpan;
            client.DefaultRequestHeaders.UserAgent.Add(HttpFeedClient.UserAgent);
        });
        services.AddSingleton<IFeedClient, HttpFeedClient>();
        services.AddSingleton(provider => SecretStoreFactory.Create(provider.GetRequiredService<IAppPaths>()));
        return services;
    }
}
