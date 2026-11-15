namespace OpenMyo.Core.Abstractions;

/// <summary>Per-user directories for settings and caches.</summary>
public interface IAppPaths
{
    /// <summary>Root for settings and protected secrets.</summary>
    string DataDirectory { get; }

    /// <summary>Root for disposable caches (icon images, card JSON).</summary>
    string CacheDirectory { get; }
}
