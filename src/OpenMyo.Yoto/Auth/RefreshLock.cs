using OpenMyo.Core.Abstractions;

namespace OpenMyo.Yoto.Auth;

/// <summary>
/// A lock shared by every OpenMyo process on this machine (the app and the Claude Desktop connector), held
/// while one of them uses the refresh token. Yoto refresh tokens are single-use, so two processes refreshing
/// with the same token at once would sign the user out. Uses an exclusively opened file, which .NET maps to
/// an OS-level lock on macOS and Windows and releases automatically if a process dies.
/// </summary>
internal sealed class RefreshLock(IAppPaths paths)
{
    private const string FileName = "yoto-refresh.lock";
    private static readonly TimeSpan RetryDelay = TimeSpan.FromMilliseconds(50);
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(30);

    /// <summary>Waits for the lock; dispose the result to release it.</summary>
    public async Task<IDisposable> AcquireAsync(CancellationToken cancellationToken)
    {
        Directory.CreateDirectory(paths.DataDirectory);
        var path = Path.Combine(paths.DataDirectory, FileName);
        var deadline = DateTimeOffset.UtcNow + Timeout;
        while (true)
        {
            try
            {
                return new FileStream(path, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
            }
            catch (IOException) when (DateTimeOffset.UtcNow < deadline)
            {
                await Task.Delay(RetryDelay, cancellationToken);
            }
        }
    }
}
