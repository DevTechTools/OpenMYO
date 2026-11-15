namespace OpenMyo.Core.CommunityIcons;

/// <summary>How far a catalog harvest has got, for a progress bar.</summary>
/// <param name="PagesDone">Listing pages read so far (including failed ones that gave up).</param>
/// <param name="TotalPages">Pages in the listing; null while unknown.</param>
public sealed record CommunityHarvestProgress(int PagesDone, int? TotalPages)
{
    /// <summary>0..1 when the total is known, otherwise null (show an indeterminate bar).</summary>
    public double? Fraction => TotalPages is > 0 and var total ? Math.Min(1.0, (double)PagesDone / total) : null;
}
