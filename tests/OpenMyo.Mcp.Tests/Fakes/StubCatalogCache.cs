using OpenMyo.Core.Abstractions;
using OpenMyo.Core.CommunityIcons;

namespace OpenMyo.Mcp.Tests.Fakes;

public sealed class StubCatalogCache(CommunityCatalog? stored) : ICommunityCatalogCache
{
    public Task<CommunityCatalog?> LoadAsync(CancellationToken cancellationToken) => Task.FromResult(stored);

    public Task SaveAsync(CommunityCatalog catalog, CancellationToken cancellationToken) => Task.CompletedTask;
}
