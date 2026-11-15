using System.Text.Json;
using OpenMyo.Core.Abstractions;
using OpenMyo.Core.CommunityIcons;

namespace OpenMyo.Yoto.CommunityIcons;

/// <summary>
/// Stores the harvested community catalog as JSON under the cache directory. Writes go to a temporary file
/// that then replaces the old one, so a crash mid-write never leaves a half-written catalog.
/// </summary>
internal sealed class CommunityCatalogFile(IAppPaths paths) : ICommunityCatalogCache
{
    private string FilePath => Path.Combine(paths.CacheDirectory, "yotoicons", "catalog.json");

    public async Task<CommunityCatalog?> LoadAsync(CancellationToken cancellationToken)
    {
        try
        {
            if (!File.Exists(FilePath))
            {
                return null;
            }

            await using var stream = File.OpenRead(FilePath);
            var dto = await JsonSerializer.DeserializeAsync(stream, CommunityCatalogJsonContext.Default.CommunityCatalogFileDto, cancellationToken);
            return dto is null ? null : ToCatalog(dto);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException or UriFormatException)
        {
            return null; // An unreadable cache is just a missing one; the catalog is harvested again.
        }
    }

    public async Task SaveAsync(CommunityCatalog catalog, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(catalog);

        var file = FilePath;
        var temporary = file + ".tmp";
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(file) ?? paths.CacheDirectory);
            await using (var stream = File.Create(temporary))
            {
                await JsonSerializer.SerializeAsync(stream, ToDto(catalog), CommunityCatalogJsonContext.Default.CommunityCatalogFileDto, cancellationToken);
            }

            File.Move(temporary, file, overwrite: true);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            // Caching is best effort; the catalog stays usable in memory.
        }
    }

    private static CommunityCatalogFileDto ToDto(CommunityCatalog catalog) => new(
        catalog.FetchedAt,
        catalog.IsComplete,
        catalog.Icons.Select(icon => new CommunityIconDto(icon.Id, icon.Title, icon.Tags, icon.Artist, icon.Downloads, icon.ImageUrl.AbsoluteUri)).ToList());

    private static CommunityCatalog ToCatalog(CommunityCatalogFileDto dto) => new(
        dto.Icons.Select(icon => new CommunityIcon(icon.Id, icon.Title, icon.Tags, icon.Artist, icon.Downloads, new Uri(icon.ImageUrl))).ToList(),
        dto.FetchedAt,
        dto.IsComplete);
}
