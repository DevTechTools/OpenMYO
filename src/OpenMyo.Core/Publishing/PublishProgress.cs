namespace OpenMyo.Core.Publishing;

/// <summary>Progress of publishing one playlist.</summary>
/// <param name="Fraction">0 to 1 across the whole operation.</param>
/// <param name="Status">Short, user-facing description of the current step.</param>
public sealed record PublishProgress(double Fraction, string Status);
