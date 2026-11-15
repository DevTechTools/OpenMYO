using Microsoft.Extensions.DependencyInjection;
using OpenMyo.Core.CommunityIcons;
using OpenMyo.Core.Feeds;
using OpenMyo.Core.Icons;
using OpenMyo.Core.Import;
using OpenMyo.Core.Publishing;
using OpenMyo.Core.Text;

namespace OpenMyo.Core;

public static class ServiceCollectionExtensions
{
    /// <summary>Registers Core services. Requires implementations of the Core abstractions from other projects.</summary>
    public static IServiceCollection AddCore(this IServiceCollection services)
    {
        services.AddSingleton<IconIndexProvider>();
        services.AddSingleton<CommunityCatalogHarvester>();
        services.AddSingleton<CommunityCatalogProvider>();
        services.AddSingleton<CommunityIconSearch>();
        services.AddSingleton<CommunityIconImporter>();
        services.AddSingleton<IIconSuggester, IconSuggester>();
        services.AddSingleton<NumberIconMaker>();
        services.AddSingleton<CommunityIconFallback>();
        services.AddSingleton<IconAutoAssigner>();
        services.AddSingleton<AiIconSearch>();
        services.AddSingleton<AiTitleRenamer>();
        services.AddSingleton<DraftBuilder>();
        services.AddSingleton<FeedImporter>();
        services.AddSingleton<ArtworkLoader>();
        services.AddSingleton<PlaylistPublisher>();
        services.AddSingleton<PlaylistMediaService>();
        return services;
    }
}
