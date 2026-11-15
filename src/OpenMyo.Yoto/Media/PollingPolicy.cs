namespace OpenMyo.Yoto.Media;

/// <summary>How long and how often to wait for Yoto's transcoder. Tests use tiny values.</summary>
internal sealed record PollingPolicy(TimeSpan InitialDelay, TimeSpan MaxDelay, TimeSpan Timeout)
{
    public static PollingPolicy Default { get; } =
        new(TimeSpan.FromMilliseconds(500), TimeSpan.FromSeconds(2), TimeSpan.FromMinutes(5));
}
