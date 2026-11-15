using Anthropic.Models.Beta;
using Anthropic.Models.Beta.Messages;
using OpenMyo.Core.Abstractions;
using OpenMyo.Core.Text;

namespace OpenMyo.Claude;

/// <summary>Asks Claude for a clean display title per track. Runs only when the user clicks.</summary>
public sealed class ClaudeTitleWriter(ClaudeClientProvider clients, ISettingsStore settingsStore) : IAiTitleWriter
{
    // A title is a few words; budget scales with the request so a runaway reply stays cheap.
    private const int BaseOutputTokens = 1024;
    private const int OutputTokensPerTrack = 64;

    public async Task<IReadOnlyList<string?>> RewriteAsync(
        string playlistTitle, IReadOnlyList<string> trackTitles, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(playlistTitle);
        ArgumentNullException.ThrowIfNull(trackTitles);

        var settings = await settingsStore.LoadAsync(cancellationToken);
        var client = await clients.GetAsync(cancellationToken);
        var response = await client.Beta.Messages.Create(BuildRequest(settings.ClaudeModel, playlistTitle, trackTitles), cancellationToken);

        if (response.StopReason == "refusal")
        {
            throw new InvalidOperationException("Claude declined to rename the tracks in this playlist.");
        }

        var json = string.Concat(response.Content.Select(block => block.Value).OfType<BetaTextBlock>().Select(text => text.Text));
        return TitleResponse.Parse(json, trackTitles.Count);
    }

    private static MessageCreateParams BuildRequest(string model, string playlistTitle, IReadOnlyList<string> trackTitles) => new()
    {
        Model = model,
        MaxTokens = BaseOutputTokens + (OutputTokensPerTrack * trackTitles.Count),
        // Server-side fallback keeps renaming working if the primary model declines a request.
        Betas = [AnthropicBeta.ServerSideFallback2026_07_01],
        Fallbacks = new Default(),
        System = new List<BetaTextBlockParam>
        {
            new() { Text = TitlePrompt.SystemPrompt, CacheControl = new BetaCacheControlEphemeral() },
        },
        OutputConfig = new BetaOutputConfig
        {
            Effort = Effort.Low,
            Format = new BetaJsonOutputFormat { Schema = TitlePrompt.Schema() },
        },
        Messages = [new BetaUserMessageParam { Content = TitlePrompt.UserMessage(playlistTitle, trackTitles) }],
    };
}
