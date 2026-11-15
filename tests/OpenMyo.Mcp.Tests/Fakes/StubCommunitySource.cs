using OpenMyo.Core.Abstractions;
using OpenMyo.Core.CommunityIcons;

namespace OpenMyo.Mcp.Tests.Fakes;

/// <summary>Community site stand-in: downloads succeed, listing is never needed because the cache is fresh.</summary>
public sealed class StubCommunitySource : ICommunityIconSource
{
    public Task<CommunityCatalogPage> GetPageAsync(int page, CancellationToken cancellationToken) =>
        throw new HttpRequestException("offline");

    public Task<ReadOnlyMemory<byte>> DownloadAsync(CommunityIcon icon, CancellationToken cancellationToken) =>
        Task.FromResult<ReadOnlyMemory<byte>>(new byte[] { 1 });
}
