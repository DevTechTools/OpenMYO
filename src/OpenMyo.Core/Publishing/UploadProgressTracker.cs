namespace OpenMyo.Core.Publishing;

/// <summary>Combines per-file upload progress into one overall value, safe to call from parallel uploads.</summary>
internal sealed class UploadProgressTracker(int fileCount, IProgress<PublishProgress>? overall)
{
    /// <summary>Uploads cover most of the work; the remainder is cover art and saving.</summary>
    private const double UploadShare = 0.95;

    private readonly double[] _fileFractions = new double[fileCount];
    private readonly Lock _gate = new();

    public IProgress<double> ForFile(int index) => new FileProgress(this, index);

    private void Report(int index, double fraction)
    {
        if (overall is null || fileCount == 0)
        {
            return;
        }

        double total;
        int done;
        lock (_gate)
        {
            _fileFractions[index] = Math.Clamp(fraction, 0, 1);
            total = _fileFractions.Sum() / fileCount;
            done = _fileFractions.Count(value => value >= 1);
        }

        overall.Report(new PublishProgress(total * UploadShare, $"Uploading {done} of {fileCount}"));
    }

    private sealed class FileProgress(UploadProgressTracker owner, int index) : IProgress<double>
    {
        public void Report(double value) => owner.Report(index, value);
    }
}
