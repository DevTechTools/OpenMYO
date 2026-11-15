using Microsoft.Extensions.Logging;

namespace OpenMyo.App.Services;

/// <summary>
/// Starts work the caller doesn't wait for (a search as the user types, a refresh after an event) without
/// losing its failure. Writing <c>_ = DoAsync()</c> instead would drop any exception silently.
/// </summary>
public static class BackgroundWork
{
    /// <param name="description">What the work is, for the log: "icon search", "saving settings".</param>
    public static void Forget(this Task task, ILogger logger, string description)
    {
        ArgumentNullException.ThrowIfNull(task);
        ArgumentNullException.ThrowIfNull(logger);
        _ = LogFailureAsync(task, logger, description);
    }

    private static async Task LogFailureAsync(Task task, ILogger logger, string description)
    {
        try
        {
            await task;
        }
        catch (OperationCanceledException)
        {
            // Cancelled on purpose, usually because newer work replaced it.
        }
        catch (Exception exception)
        {
            // UI boundary: nobody awaits this work, so logging is the only way the failure is seen.
            logger.LogError(exception, "Background work failed: {Description}", description);
        }
    }
}
