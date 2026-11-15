using System.Collections.Concurrent;
using OpenMyo.Core.Abstractions;

namespace OpenMyo.Core.CommunityIcons;

/// <summary>
/// Reads every listing page of the community site into one <see cref="CommunityCatalog"/>. Page 1 says how
/// many icons there are; the rest are read a few at a time (the site is a free hobby project), each retried
/// briefly. Pages that still fail are skipped and the catalog is marked incomplete.
/// </summary>
public sealed class CommunityCatalogHarvester(ICommunityIconSource source)
{
    /// <summary>Pages read at once: fast enough for ~1,000 pages, gentle enough for a small site.</summary>
    public const int MaxParallelPages = 4;

    private const int MaxRetries = 2;
    private const int FallbackPageLimit = 5000;
    private static readonly TimeSpan RetryDelay = TimeSpan.FromMilliseconds(300);

    /// <exception cref="HttpRequestException">Page 1 could not be read, so nothing is known about the site.</exception>
    public async Task<CommunityCatalog> HarvestAsync(IProgress<CommunityHarvestProgress>? progress, CancellationToken cancellationToken)
    {
        var first = await FetchWithRetryAsync(1, throwOnFailure: true, cancellationToken)
            ?? throw new HttpRequestException("yotoicons.com returned nothing for the first page.");
        var run = new HarvestRun(progress, TotalPages(first));
        run.Add(1, first.Icons);

        if (run.TotalPages is { } totalPages)
        {
            await FetchPagesAsync(Enumerable.Range(2, Math.Max(0, totalPages - 1)), run, cancellationToken);
        }
        else
        {
            await FetchUntilEmptyAsync(run, cancellationToken);
        }

        return run.ToCatalog();
    }

    private static int? TotalPages(CommunityCatalogPage first) =>
        first.TotalCount is { } total && first.Icons.Count > 0
            ? (int)Math.Ceiling(total / (double)first.Icons.Count)
            : null;

    // No total on the page: read waves of pages until one comes back empty, or a whole wave fails.
    private async Task FetchUntilEmptyAsync(HarvestRun run, CancellationToken cancellationToken)
    {
        for (var start = 2; start <= FallbackPageLimit; start += MaxParallelPages)
        {
            var wave = Enumerable.Range(start, MaxParallelPages).ToList();
            await FetchPagesAsync(wave, run, cancellationToken);
            if (run.ReachedEnd(wave))
            {
                return;
            }
        }
    }

    private async Task FetchPagesAsync(IEnumerable<int> pages, HarvestRun run, CancellationToken cancellationToken)
    {
        var options = new ParallelOptions { MaxDegreeOfParallelism = MaxParallelPages, CancellationToken = cancellationToken };
        await Parallel.ForEachAsync(pages, options, async (page, token) =>
        {
            var result = await FetchWithRetryAsync(page, throwOnFailure: false, token);
            if (result is null)
            {
                run.Fail();
            }
            else
            {
                run.Add(page, result.Icons);
            }
        });
    }

    private async Task<CommunityCatalogPage?> FetchWithRetryAsync(int page, bool throwOnFailure, CancellationToken cancellationToken)
    {
        for (var attempt = 0; ; attempt++)
        {
            try
            {
                return await source.GetPageAsync(page, cancellationToken);
            }
            catch (HttpRequestException) when (attempt < MaxRetries)
            {
                await Task.Delay(RetryDelay * (attempt + 1), cancellationToken);
            }
            catch (HttpRequestException) when (!throwOnFailure)
            {
                return null;
            }
        }
    }

    /// <summary>The thread-safe tally of one harvest.</summary>
    private sealed class HarvestRun(IProgress<CommunityHarvestProgress>? progress, int? totalPages)
    {
        private readonly ConcurrentDictionary<int, IReadOnlyList<CommunityIcon>> _pages = new();
        private int _done;
        private int _failed;

        public int? TotalPages => totalPages;

        public void Add(int page, IReadOnlyList<CommunityIcon> icons)
        {
            _pages[page] = icons;
            Report();
        }

        public void Fail()
        {
            Interlocked.Increment(ref _failed);
            Report();
        }

        public bool ReachedEnd(IReadOnlyList<int> wave) =>
            wave.Any(page => _pages.TryGetValue(page, out var icons) && icons.Count == 0) || wave.All(page => !_pages.ContainsKey(page));

        // Pages are joined in order; an icon that moved pages mid-harvest is kept once.
        public CommunityCatalog ToCatalog() => new(
            _pages.OrderBy(pair => pair.Key).SelectMany(pair => pair.Value).DistinctBy(icon => icon.Id, StringComparer.Ordinal).ToList(),
            DateTimeOffset.UtcNow,
            IsComplete: Volatile.Read(ref _failed) == 0);

        private void Report() => progress?.Report(new CommunityHarvestProgress(Interlocked.Increment(ref _done), totalPages));
    }
}
