using OpenMyo.Core.Publishing;
using OpenMyo.Core.Tests.Fakes;

namespace OpenMyo.Core.Tests.Publishing;

public class UploadProgressTrackerTests
{
    [Fact]
    public void ForFile_PartialProgress_ReportsAveragedFractionScaledToUploadShare()
    {
        var overall = new RecordingProgress<PublishProgress>();
        var tracker = new UploadProgressTracker(2, overall);

        tracker.ForFile(0).Report(1);

        var report = Assert.Single(overall.Reports);
        Assert.Equal(0.5 * 0.95, report.Fraction, precision: 10);
        Assert.Equal("Uploading 1 of 2", report.Status);
    }

    [Fact]
    public void ForFile_OutOfRangeValue_IsClamped()
    {
        var overall = new RecordingProgress<PublishProgress>();
        var tracker = new UploadProgressTracker(1, overall);

        tracker.ForFile(0).Report(5);

        Assert.Equal(0.95, Assert.Single(overall.Reports).Fraction, precision: 10);
    }

    [Fact]
    public void ForFile_NoOverallProgress_DoesNotThrow()
    {
        var tracker = new UploadProgressTracker(1, null);

        tracker.ForFile(0).Report(0.5);
    }
}
