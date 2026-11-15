namespace OpenMyo.Core.Feeds;

/// <summary>
/// Combines per-episode download progress into one value, safe to call from parallel downloads. Each update also
/// names its episode so the preview can show every download that is moving.
/// </summary>
internal sealed class DownloadProgressTracker(IReadOnlyList<FeedEpisode> episodes, IProgress<FeedDownloadProgress>? overall)
{
    // Downloads report every buffer; passing on only whole-percent steps keeps the UI thread from flooding.
    private const double MinimumStep = 0.01;

    private readonly double[] _fractions = new double[episodes.Count];
    private readonly Lock _gate = new();

    public IProgress<double> ForEpisode(int index) => new EpisodeProgress(this, index);

    private void Report(int index, double fraction)
    {
        if (overall is null || episodes.Count == 0)
        {
            return;
        }

        var clamped = Math.Clamp(fraction, 0, 1);
        double total;
        int done;
        int active;
        lock (_gate)
        {
            if (clamped < 1 && clamped - _fractions[index] < MinimumStep)
            {
                return;
            }

            _fractions[index] = clamped;
            total = _fractions.Sum() / episodes.Count;
            done = _fractions.Count(value => value >= 1);
            active = _fractions.Count(value => value is > 0 and < 1);
        }

        var status = active > 0
            ? $"Downloaded {done} of {episodes.Count} · {active} downloading now"
            : $"Downloaded {done} of {episodes.Count}";
        overall.Report(new FeedDownloadProgress(total, status, episodes[index], clamped));
    }

    private sealed class EpisodeProgress(DownloadProgressTracker owner, int index) : IProgress<double>
    {
        public void Report(double value) => owner.Report(index, value);
    }
}
