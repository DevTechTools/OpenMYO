using OpenMyo.Core.Abstractions;
using OpenMyo.Yoto.Auth;

namespace OpenMyo.Yoto.Tests.Auth;

public sealed class RefreshLockTests : IDisposable
{
    private readonly TempPaths _paths = new();

    [Fact]
    public async Task AcquireAsync_WhileHeldElsewhere_WaitsForRelease()
    {
        var first = await new RefreshLock(_paths).AcquireAsync(CancellationToken.None);
        var second = new RefreshLock(_paths).AcquireAsync(CancellationToken.None);

        await Task.Delay(200);
        Assert.False(second.IsCompleted);

        first.Dispose();
        (await second.WaitAsync(TimeSpan.FromSeconds(5))).Dispose();
    }

    public void Dispose() => Directory.Delete(_paths.DataDirectory, recursive: true);

    private sealed class TempPaths : IAppPaths
    {
        public string DataDirectory { get; } = Path.Combine(Path.GetTempPath(), "openmyo-lock-" + Guid.NewGuid().ToString("N"));

        public string CacheDirectory => DataDirectory;
    }
}
