namespace OpenMyo.Core.Players;

/// <summary>Whether a player is playing, as it last reported.</summary>
public enum PlayerStatus
{
    Unknown,
    Stopped,
    Playing,
    Paused,
}

/// <summary>
/// What a player is doing, built up from its event messages. Each message may carry only the fields that
/// changed, so a new message is merged over the previous state with <see cref="Merge"/>.
/// </summary>
public sealed record PlayerState
{
    /// <summary>Yoto players have 16 volume steps; a parental limit (<see cref="VolumeMax"/>) may cap them lower.</summary>
    public const int VolumeSteps = 16;

    public static readonly PlayerState Empty = new();

    public string? CardId { get; init; }

    public string? ChapterKey { get; init; }

    public string? TrackKey { get; init; }

    public string? TrackTitle { get; init; }

    public TimeSpan Position { get; init; }

    public TimeSpan Length { get; init; }

    public PlayerStatus Status { get; init; }

    /// <summary>Volume in player steps (0–16), or null until the player reports it.</summary>
    public int? Volume { get; init; }

    /// <summary>The highest step the player allows (its parental limit), or null until reported.</summary>
    public int? VolumeMax { get; init; }

    /// <summary>True when a card is loaded; players report the card as "none" once stopped.</summary>
    public bool HasCard => !string.IsNullOrEmpty(CardId) && !string.Equals(CardId, NoCard, StringComparison.Ordinal);

    /// <summary>True when the player is in the middle of something (playing or paused) that must not be overridden.</summary>
    public bool IsBusy => HasCard && Status is PlayerStatus.Playing or PlayerStatus.Paused;

    /// <summary>The card id a player reports when nothing is loaded.</summary>
    public const string NoCard = "none";

    /// <summary>Takes every field <paramref name="update"/> has and keeps the rest.</summary>
    public PlayerState Merge(PlayerStateUpdate update)
    {
        ArgumentNullException.ThrowIfNull(update);
        return new PlayerState
        {
            CardId = update.CardId ?? CardId,
            ChapterKey = update.ChapterKey ?? ChapterKey,
            TrackKey = update.TrackKey ?? TrackKey,
            TrackTitle = update.TrackTitle ?? TrackTitle,
            Position = update.Position ?? Position,
            Length = update.Length ?? Length,
            Status = update.Status ?? Status,
            Volume = update.Volume ?? Volume,
            VolumeMax = update.VolumeMax ?? VolumeMax,
        };
    }
}

/// <summary>The fields one player event carried; null means "not in this message".</summary>
public sealed record PlayerStateUpdate(
    string? CardId = null,
    string? ChapterKey = null,
    string? TrackKey = null,
    string? TrackTitle = null,
    TimeSpan? Position = null,
    TimeSpan? Length = null,
    PlayerStatus? Status = null,
    int? Volume = null,
    int? VolumeMax = null);
