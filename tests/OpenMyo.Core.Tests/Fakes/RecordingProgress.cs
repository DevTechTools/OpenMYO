namespace OpenMyo.Core.Tests.Fakes;

/// <summary>Synchronous <see cref="IProgress{T}"/> (unlike <see cref="Progress{T}"/>, which posts) that keeps every report.</summary>
public sealed class RecordingProgress<T> : IProgress<T>
{
    private readonly Lock _gate = new();
    private readonly List<T> _reports = [];

    public IReadOnlyList<T> Reports
    {
        get
        {
            lock (_gate)
            {
                return [.. _reports];
            }
        }
    }

    public void Report(T value)
    {
        lock (_gate)
        {
            _reports.Add(value);
        }
    }
}
