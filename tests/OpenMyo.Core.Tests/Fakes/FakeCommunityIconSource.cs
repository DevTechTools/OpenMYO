using System.Collections.Concurrent;
using OpenMyo.Core.Abstractions;
using OpenMyo.Core.CommunityIcons;

namespace OpenMyo.Core.Tests.Fakes;

/// <summary>A listing of <c>pageCount</c> pages of <c>pageSize</c> icons; records calls and peak concurrency.</summary>
public sealed class FakeCommunityIconSource(int pageCount, int pageSize = 3) : ICommunityIconSource
{
    private readonly ConcurrentDictionary<int, int> _calls = new();
    private int _inFlight;
    private int _peak;

    /// <summary>How many times each page fails before succeeding; int.MaxValue fails forever.</summary>
    public ConcurrentDictionary<int, int> Failures { get; } = new();

    public TimeSpan Delay { get; init; } = TimeSpan.FromMilliseconds(5);

    public bool ReportsTotal { get; init; } = true;

    public int PeakConcurrency => Volatile.Read(ref _peak);

    public int TotalCalls => _calls.Values.Sum();

    public int CallsFor(int page) => _calls.GetValueOrDefault(page);

    public static CommunityIcon Icon(string id, string title, int downloads = 0) =>
        new(id, title, title.Split(' '), "artist", downloads, new Uri($"https://yotoicons.com/static/uploads/{id}.png"));

    public async Task<CommunityCatalogPage> GetPageAsync(int page, CancellationToken cancellationToken)
    {
        var call = _calls.AddOrUpdate(page, 1, (_, count) => count + 1);
        var inFlight = Interlocked.Increment(ref _inFlight);
        InterlockedMax(ref _peak, inFlight);
        try
        {
            await Task.Delay(Delay, cancellationToken);
            if (call <= Failures.GetValueOrDefault(page))
            {
                throw new HttpRequestException($"Page {page} failed.");
            }

            return new CommunityCatalogPage(IconsOn(page), ReportsTotal ? pageCount * pageSize : null);
        }
        finally
        {
            Interlocked.Decrement(ref _inFlight);
        }
    }

    public Task<ReadOnlyMemory<byte>> DownloadAsync(CommunityIcon icon, CancellationToken cancellationToken) =>
        Task.FromResult<ReadOnlyMemory<byte>>(new byte[] { 1 });

    private List<CommunityIcon> IconsOn(int page) => page > pageCount
        ? []
        : Enumerable.Range(0, pageSize).Select(i => Icon($"{page}-{i}", $"icon {page} {i}")).ToList();

    private static void InterlockedMax(ref int target, int value)
    {
        int current;
        while ((current = Volatile.Read(ref target)) < value && Interlocked.CompareExchange(ref target, value, current) != current)
        {
        }
    }
}
