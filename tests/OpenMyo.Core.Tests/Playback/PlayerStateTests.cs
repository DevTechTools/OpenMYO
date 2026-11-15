using OpenMyo.Core.Players;

namespace OpenMyo.Core.Tests.Playback;

public class PlayerStateTests
{
    [Fact]
    public void Merge_PartialUpdate_KeepsFieldsItDoesNotCarry()
    {
        var state = PlayerState.Empty.Merge(new PlayerStateUpdate(CardId: "card", TrackTitle: "Moon", Status: PlayerStatus.Playing, Volume: 40));

        var merged = state.Merge(new PlayerStateUpdate(Position: TimeSpan.FromSeconds(12), Status: PlayerStatus.Paused));

        Assert.Equal(("card", "Moon", PlayerStatus.Paused, 40, TimeSpan.FromSeconds(12)),
            (merged.CardId, merged.TrackTitle, merged.Status, merged.Volume, merged.Position));
    }

    [Theory]
    [InlineData("card", PlayerStatus.Playing, true)]
    [InlineData("card", PlayerStatus.Paused, true)]
    [InlineData("card", PlayerStatus.Stopped, false)]
    [InlineData("none", PlayerStatus.Paused, false)]
    [InlineData(null, PlayerStatus.Playing, false)]
    public void IsBusy_CardAndStatus_TrueOnlyWhenPlayingOrPausedOnACard(string? cardId, PlayerStatus status, bool expected)
    {
        var state = new PlayerState { CardId = cardId, Status = status };

        Assert.Equal(expected, state.IsBusy);
    }
}
