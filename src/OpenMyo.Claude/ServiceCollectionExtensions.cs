using Microsoft.Extensions.DependencyInjection;
using OpenMyo.Core.Icons;
using OpenMyo.Core.Text;

namespace OpenMyo.Claude;

public static class ServiceCollectionExtensions
{
    /// <summary>Registers the optional Claude features (icon keywords, track titles). Inactive until an API key is stored.</summary>
    public static IServiceCollection AddClaude(this IServiceCollection services)
    {
        services.AddSingleton<ClaudeClientProvider>();
        services.AddSingleton<IIconKeywordGenerator, ClaudeKeywordGenerator>();
        services.AddSingleton<IAiTitleWriter, ClaudeTitleWriter>();
        return services;
    }
}
