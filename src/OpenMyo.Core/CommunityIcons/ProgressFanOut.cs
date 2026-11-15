namespace OpenMyo.Core.CommunityIcons;

/// <summary>
/// Lets every caller waiting on one shared operation watch its progress. A late subscriber immediately gets
/// the latest report, so a progress bar opened mid-harvest does not start empty.
/// </summary>
internal sealed class ProgressFanOut<T> : IProgress<T>
    where T : class
{
    private readonly Lock _gate = new();
    private readonly List<IProgress<T>> _subscribers = [];
    private T? _latest;

    public IDisposable Subscribe(IProgress<T>? subscriber)
    {
        if (subscriber is null)
        {
            return new Subscription(this, null);
        }

        T? latest;
        lock (_gate)
        {
            _subscribers.Add(subscriber);
            latest = _latest;
        }

        if (latest is not null)
        {
            subscriber.Report(latest);
        }

        return new Subscription(this, subscriber);
    }

    public void Report(T value)
    {
        IProgress<T>[] subscribers;
        lock (_gate)
        {
            _latest = value;
            subscribers = [.. _subscribers];
        }

        foreach (var subscriber in subscribers)
        {
            subscriber.Report(value);
        }
    }

    private void Unsubscribe(IProgress<T> subscriber)
    {
        lock (_gate)
        {
            _subscribers.Remove(subscriber);
        }
    }

    private sealed class Subscription(ProgressFanOut<T> owner, IProgress<T>? subscriber) : IDisposable
    {
        public void Dispose()
        {
            if (subscriber is not null)
            {
                owner.Unsubscribe(subscriber);
            }
        }
    }
}
