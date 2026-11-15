namespace OpenMyo.Core.Icons;

/// <summary>An icon and how well it matched a query, from 0 (no match) to 1 (exact).</summary>
public sealed record IconMatch(DisplayIcon Icon, double Score);
