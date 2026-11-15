using OpenMyo.Core.Icons;

namespace OpenMyo.Core.Abstractions;

/// <summary>Source of display icons: Yoto's public library plus the user's uploads.</summary>
public interface IIconCatalog
{
    /// <summary>Returns every available icon. Implementations should cache aggressively.</summary>
    Task<IReadOnlyList<DisplayIcon>> GetIconsAsync(CancellationToken cancellationToken);
}
