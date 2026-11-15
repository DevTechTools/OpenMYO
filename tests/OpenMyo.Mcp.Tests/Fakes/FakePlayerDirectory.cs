using OpenMyo.Core.Abstractions;
using OpenMyo.Core.Players;

namespace OpenMyo.Mcp.Tests.Fakes;

public sealed class FakePlayerDirectory(params YotoPlayer[] players) : IPlayerDirectory
{
    public Task<IReadOnlyList<YotoPlayer>> ListAsync(CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<YotoPlayer>>(players);
}
