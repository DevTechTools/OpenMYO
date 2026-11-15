using OpenMyo.Core.Players;
using OpenMyo.Yoto.Devices;

namespace OpenMyo.Yoto.Tests.Devices;

public class PlayerMessagesTests
{
    [Fact]
    public void StartPayload_Target_UsesCardLinkAndWholeSeconds()
    {
        var json = PlayerMessages.StartPayload(new PlayTarget("abc12", "02", "02", TimeSpan.FromSeconds(12.7)));

        Assert.Equal("""{"uri":"https://yoto.io/abc12","chapterKey":"02","trackKey":"02","secondsIn":12,"cutOff":0,"anyButtonStop":false}""", json);
    }

    [Theory]
    [InlineData(PlayerCommand.Pause, "device/d1/command/card/pause")]
    [InlineData(PlayerCommand.Resume, "device/d1/command/card/resume")]
    [InlineData(PlayerCommand.Stop, "device/d1/command/card/stop")]
    public void CommandName_Command_MapsToTopic(PlayerCommand command, string topic)
    {
        Assert.Equal(topic, PlayerMessages.Command("d1", PlayerMessages.CommandName(command)));
    }

    [Theory]
    [InlineData(40, """{"volume":100}""")]
    [InlineData(-3, """{"volume":0}""")]
    [InlineData(10, """{"volume":63}""")]
    [InlineData(1, """{"volume":7}""")]
    public void VolumePayload_Step_IsSentAsPercentThatLandsOnThatStep(int level, string json)
    {
        Assert.Equal(json, PlayerMessages.VolumePayload(level));
    }

    [Fact]
    public void ParseEvents_FullMessage_ReadsEveryField()
    {
        var update = PlayerMessages.ParseEvents("""
            {"cardId":"abc12","chapterKey":"02","trackKey":"02","trackTitle":"The Moon","position":31,
             "trackLength":240,"playbackStatus":"playing","volume":8,"volumeMax":13,"eventUtc":1700000000}
            """);

        Assert.Equal(
            new PlayerStateUpdate("abc12", "02", "02", "The Moon", TimeSpan.FromSeconds(31), TimeSpan.FromSeconds(240), PlayerStatus.Playing, 8, 13),
            update);
    }

    [Fact]
    public void ParseEvents_PartialMessage_LeavesMissingFieldsNull()
    {
        var update = PlayerMessages.ParseEvents("""{"playbackStatus":"paused"}""");

        Assert.Equal(new PlayerStateUpdate(Status: PlayerStatus.Paused), update);
    }
}
