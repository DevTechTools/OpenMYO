using OpenMyo.Core.Players;
using OpenMyo.Yoto.Devices;
using OpenMyo.Yoto.Tests.Support;

namespace OpenMyo.Yoto.Tests.Devices;

public class YotoPlayerDirectoryTests
{
    [Fact]
    public async Task ListAsync_Devices_MapsNameAndOnlineSortedByName()
    {
        var handler = new StubHttpHandler().On("GET /device-v2/devices/mine", """
            {"devices":[
              {"deviceId":"d2","name":"Kitchen","online":false},
              {"deviceId":"d1","name":"Bedroom","online":true},
              {"deviceId":"","name":"Broken"}
            ]}
            """);
        var directory = new YotoPlayerDirectory(new StubHttpClientFactory(handler));

        var players = await directory.ListAsync(CancellationToken.None);

        Assert.Equal([new YotoPlayer("d1", "Bedroom", true), new YotoPlayer("d2", "Kitchen", false)], players);
    }
}
