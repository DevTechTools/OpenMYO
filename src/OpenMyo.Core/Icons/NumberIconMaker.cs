using System.Collections.Concurrent;
using OpenMyo.Core.Abstractions;

namespace OpenMyo.Core.Icons;

/// <summary>
/// Finds an icon for each track number: Yoto's own when it exists, otherwise one drawn by
/// <see cref="NumberIconPainter"/> and uploaded to the user's icons once. Uploads are titled
/// <see cref="NumberIcons.DrawnTitle"/> so later runs find them in the catalog instead of uploading again.
/// </summary>
public sealed class NumberIconMaker(IMediaUploader uploader, IconIndexProvider indexProvider)
{
    private const int UploadParallelism = 4;
    private const string ContentType = "image/png";

    private readonly ConcurrentDictionary<int, string> _uploaded = new();

    /// <summary>Returns a media id per number; numbers outside 1–999 are omitted.</summary>
    public async Task<IReadOnlyDictionary<int, string>> GetOrCreateNumberIconsAsync(IEnumerable<int> numbers, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(numbers);

        var index = await indexProvider.GetAsync(cancellationToken);
        var result = new ConcurrentDictionary<int, string>();
        var toDraw = new List<int>();
        foreach (var number in numbers.Distinct().Where(n => n is >= 1 and <= NumberIconPainter.MaxNumber))
        {
            var known = index.NumberIcon(number)?.MediaId ?? _uploaded.GetValueOrDefault(number);
            if (known is null)
            {
                toDraw.Add(number);
            }
            else
            {
                result[number] = known;
            }
        }

        var options = new ParallelOptions { MaxDegreeOfParallelism = UploadParallelism, CancellationToken = cancellationToken };
        await Parallel.ForEachAsync(toDraw, options, async (number, token) =>
        {
            var mediaId = await uploader.UploadIconAsync(NumberIconPainter.Paint(number), ContentType, NumberIcons.DrawnTitle(number), token);
            _uploaded[number] = result[number] = mediaId;
        });

        if (toDraw.Count > 0)
        {
            indexProvider.Invalidate();
        }

        return result;
    }
}
