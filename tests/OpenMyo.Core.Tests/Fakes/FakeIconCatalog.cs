using OpenMyo.Core.Abstractions;
using OpenMyo.Core.Icons;

namespace OpenMyo.Core.Tests.Fakes;

public sealed class FakeIconCatalog(IReadOnlyList<DisplayIcon> icons) : IIconCatalog
{
    public int CallCount { get; private set; }

    public Task<IReadOnlyList<DisplayIcon>> GetIconsAsync(CancellationToken cancellationToken)
    {
        CallCount++;
        return Task.FromResult(icons);
    }
}
