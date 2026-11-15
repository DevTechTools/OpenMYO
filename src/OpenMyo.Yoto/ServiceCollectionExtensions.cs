using Microsoft.Extensions.DependencyInjection;
using OpenMyo.Core.Abstractions;
using OpenMyo.Yoto.Auth;
using OpenMyo.Yoto.CommunityIcons;
using OpenMyo.Yoto.Content;
using OpenMyo.Yoto.Devices;
using OpenMyo.Yoto.Http;
using OpenMyo.Yoto.Icons;
using OpenMyo.Yoto.Media;

namespace OpenMyo.Yoto;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers the Yoto client. The host must also register <see cref="ISecretStore"/>,
    /// <see cref="ISettingsStore"/> and <see cref="IAppPaths"/>.
    /// </summary>
    public static IServiceCollection AddYoto(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddHttpClient(YotoHttpClients.Auth, client => client.BaseAddress = YotoHttpClients.LoginBaseAddress);
        // Large audio uploads outlast the default 100 s timeout; callers cancel through their token.
        services.AddHttpClient(YotoHttpClients.Upload, client => client.Timeout = Timeout.InfiniteTimeSpan);
        services.AddTransient<RetryHandler>();
        services.AddTransient<YotoAuthHandler>();
        services.AddHttpClient(YotoHttpClients.Api, client => client.BaseAddress = YotoHttpClients.ApiBaseAddress)
            .AddHttpMessageHandler<RetryHandler>()
            .AddHttpMessageHandler<YotoAuthHandler>();

        services.AddSingleton<YotoTokenClient>();
        services.AddSingleton<IBrowserLauncher, SystemBrowserLauncher>();
        services.AddSingleton<LoopbackAuthorizer>();
        services.AddSingleton<RefreshLock>();
        services.AddSingleton<YotoSession>();
        services.AddSingleton<IYotoSession>(provider => provider.GetRequiredService<YotoSession>());
        services.AddSingleton<IAccessTokenSource>(provider => provider.GetRequiredService<YotoSession>());

        services.AddSingleton<CardCache>();
        services.AddSingleton<IPlaylistRepository, YotoPlaylistRepository>();
        services.AddSingleton<ITrackStreamSource, YotoTrackStreams>();
        services.AddSingleton<IPlayerDirectory, YotoPlayerDirectory>();
        services.AddSingleton<IPlayerRemote, YotoPlayerRemote>();
        services.AddSingleton<YotoIconCatalog>();
        services.AddSingleton<IIconCatalog>(provider => provider.GetRequiredService<YotoIconCatalog>());

        services.AddHttpClient(YotoIconsClient.HttpClientName, client =>
        {
            client.BaseAddress = YotoIconsClient.BaseAddress;
            client.Timeout = TimeSpan.FromSeconds(15);
        });
        services.AddSingleton<ICommunityIconSource, YotoIconsClient>();
        services.AddSingleton<ICommunityCatalogCache, CommunityCatalogFile>();
        services.AddSingleton<IMediaUploader>(provider => new YotoMediaUploader(
            provider.GetRequiredService<IHttpClientFactory>(),
            provider.GetRequiredService<YotoIconCatalog>()));
        return services;
    }
}
