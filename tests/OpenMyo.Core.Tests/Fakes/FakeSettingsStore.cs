using OpenMyo.Core.Abstractions;

namespace OpenMyo.Core.Tests.Fakes;

public sealed class FakeSettingsStore(AppSettings? initial = null) : ISettingsStore
{
    public Task<AppSettings> LoadAsync(CancellationToken cancellationToken) => Task.FromResult(initial ?? new AppSettings());

    public Task SaveAsync(AppSettings settings, CancellationToken cancellationToken) => Task.CompletedTask;
}
