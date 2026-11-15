using OpenMyo.Core.Players;

namespace OpenMyo.Core.Abstractions;

/// <summary>The Yoto players on the signed-in account.</summary>
public interface IPlayerDirectory
{
    /// <summary>Lists every player with its name and whether it is online now.</summary>
    Task<IReadOnlyList<YotoPlayer>> ListAsync(CancellationToken cancellationToken);
}
