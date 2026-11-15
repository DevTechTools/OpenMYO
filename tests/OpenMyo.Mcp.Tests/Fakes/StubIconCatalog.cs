using OpenMyo.Core.Abstractions;
using OpenMyo.Core.Icons;

namespace OpenMyo.Mcp.Tests.Fakes;

public sealed class StubIconCatalog(IReadOnlyList<DisplayIcon> icons) : IIconCatalog
{
    public Task<IReadOnlyList<DisplayIcon>> GetIconsAsync(CancellationToken cancellationToken) => Task.FromResult(icons);
}
