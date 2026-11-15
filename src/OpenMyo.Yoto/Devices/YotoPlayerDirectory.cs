using OpenMyo.Core.Abstractions;
using OpenMyo.Core.Players;
using OpenMyo.Yoto.Http;
using OpenMyo.Yoto.Json;

namespace OpenMyo.Yoto.Devices;

/// <summary>Lists players with <c>GET /device-v2/devices/mine</c> (scope <c>family:devices:view</c>).</summary>
internal sealed class YotoPlayerDirectory(IHttpClientFactory httpClientFactory) : IPlayerDirectory
{
    private const string UnnamedPlayer = "Yoto player";

    public async Task<IReadOnlyList<YotoPlayer>> ListAsync(CancellationToken cancellationToken)
    {
        using var client = httpClientFactory.CreateClient(YotoHttpClients.Api);
        using var response = await client.GetAsync("device-v2/devices/mine", cancellationToken);
        var body = await YotoResponse.ReadObjectAsync(response, cancellationToken);

        var players = new List<YotoPlayer>();
        foreach (var device in body.GetArray("devices") ?? [])
        {
            if (device.GetString("deviceId") is { Length: > 0 } id)
            {
                var name = device.GetString("name") is { Length: > 0 } given ? given : UnnamedPlayer;
                var online = device?["online"]?.GetValueKind() == System.Text.Json.JsonValueKind.True;
                players.Add(new YotoPlayer(id, name, online));
            }
        }

        return players.OrderBy(player => player.Name, StringComparer.CurrentCultureIgnoreCase).ToList();
    }
}
