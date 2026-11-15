using OpenMyo.Core.Abstractions;
using OpenMyo.Core.CommunityIcons;

namespace OpenMyo.Core.Tests.Fakes;

public sealed class FakeCommunityCatalogCache(CommunityCatalog? stored = null) : ICommunityCatalogCache
{
    public CommunityCatalog? Stored { get; private set; } = stored;

    public int SaveCount { get; private set; }

    public Task<CommunityCatalog?> LoadAsync(CancellationToken cancellationToken) => Task.FromResult(Stored);

    public Task SaveAsync(CommunityCatalog catalog, CancellationToken cancellationToken)
    {
        Stored = catalog;
        SaveCount++;
        return Task.CompletedTask;
    }
}
