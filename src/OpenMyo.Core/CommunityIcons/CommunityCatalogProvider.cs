using OpenMyo.Core.Abstractions;

namespace OpenMyo.Core.CommunityIcons;

/// <summary>
/// Owns the local community catalog: loads it from disk when it is under a week old, otherwise harvests the
/// site once and saves it. Every caller shares one in-flight load, and each can watch its progress. The
/// load is not tied to any caller's token, so cancelling one search never wastes a half-done harvest.
/// </summary>
public sealed class CommunityCatalogProvider(CommunityCatalogHarvester harvester, ICommunityCatalogCache cache)
{
    private static readonly TimeSpan CacheLifetime = TimeSpan.FromDays(7);

    private readonly Lock _gate = new();
    private readonly ProgressFanOut<CommunityHarvestProgress> _progress = new();
    private Task<CommunityCatalogIndex>? _load;
    private CommunityCatalogIndex? _ready;

    /// <summary>True once the catalog is loaded and searches are instant.</summary>
    public bool IsReady => Volatile.Read(ref _ready) is not null;

    /// <summary>Icons in the loaded catalog; 0 until <see cref="IsReady"/>.</summary>
    public int Count => Volatile.Read(ref _ready)?.Count ?? 0;

    /// <summary>
    /// Starts (or joins) loading the catalog, e.g. when the icon picker opens, so the first search is instant.
    /// <paramref name="progress"/> hears about harvest pages only when the site is actually being read.
    /// </summary>
    /// <exception cref="HttpRequestException">The site is unreachable and there is no cached catalog.</exception>
    public async Task WarmUpAsync(IProgress<CommunityHarvestProgress>? progress, CancellationToken cancellationToken) =>
        await GetAsync(progress, cancellationToken);

    /// <inheritdoc cref="WarmUpAsync"/>
    public async Task<CommunityCatalogIndex> GetAsync(IProgress<CommunityHarvestProgress>? progress, CancellationToken cancellationToken)
    {
        using var subscription = _progress.Subscribe(progress);
        Task<CommunityCatalogIndex> load;
        lock (_gate)
        {
            if (_load is null || _load.IsFaulted || _load.IsCanceled)
            {
                _load = Task.Run(LoadAsync, CancellationToken.None);
            }

            load = _load;
        }

        return await load.WaitAsync(cancellationToken);
    }

    private async Task<CommunityCatalogIndex> LoadAsync()
    {
        var cached = await cache.LoadAsync(CancellationToken.None);
        var catalog = cached is not null && IsFresh(cached) ? cached : await HarvestAsync(cached);
        var index = CommunityCatalogIndex.Build(catalog);
        Volatile.Write(ref _ready, index);
        return index;
    }

    private static bool IsFresh(CommunityCatalog catalog) =>
        catalog.IsComplete && DateTimeOffset.UtcNow - catalog.FetchedAt < CacheLifetime;

    private async Task<CommunityCatalog> HarvestAsync(CommunityCatalog? stale)
    {
        CommunityCatalog harvested;
        try
        {
            harvested = await harvester.HarvestAsync(_progress, CancellationToken.None);
        }
        catch (HttpRequestException) when (stale is not null)
        {
            return stale; // Offline: an old catalog beats none.
        }

        if (!harvested.IsComplete && stale is not null)
        {
            harvested = harvested.MergedWith(stale);
        }

        await cache.SaveAsync(harvested, CancellationToken.None);
        return harvested;
    }
}
