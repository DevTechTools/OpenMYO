using Anthropic.Models.Beta;
using Anthropic.Models.Beta.Messages;
using OpenMyo.Core.Abstractions;
using OpenMyo.Core.Icons;

namespace OpenMyo.Claude;

/// <summary>
/// Asks Claude for drawable keywords per track. Claude never sees icon ids: it returns words, and the
/// local trigram index does the matching. That keeps requests small and the feature optional.
/// </summary>
public sealed class ClaudeKeywordGenerator(ClaudeClientProvider clients, ISettingsStore settingsStore) : IIconKeywordGenerator
{
    // Output is a few words per track; budget scales with the request so a runaway reply stays cheap.
    private const int BaseOutputTokens = 1024;
    private const int OutputTokensPerTrack = 48;

    public Task<bool> IsAvailableAsync(CancellationToken cancellationToken) => clients.HasKeyAsync(cancellationToken);

    public async Task<IReadOnlyList<IReadOnlyList<string>>> GenerateAsync(
        IconSuggestionRequest request, IReadOnlyList<string> vocabulary, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(vocabulary);

        var settings = await settingsStore.LoadAsync(cancellationToken);
        var client = await clients.GetAsync(cancellationToken);
        var response = await client.Beta.Messages.Create(BuildRequest(settings.ClaudeModel, request, vocabulary), cancellationToken);

        if (response.StopReason == "refusal")
        {
            throw new InvalidOperationException("Claude declined to suggest keywords for this playlist.");
        }

        var json = string.Concat(response.Content.Select(block => block.Value).OfType<BetaTextBlock>().Select(text => text.Text));
        return KeywordResponse.Parse(json, request.TrackTitles.Count);
    }

    private static MessageCreateParams BuildRequest(string model, IconSuggestionRequest request, IReadOnlyList<string> vocabulary) => new()
    {
        Model = model,
        MaxTokens = BaseOutputTokens + (OutputTokensPerTrack * request.TrackTitles.Count),
        // Server-side fallback keeps suggestions working if the primary model declines a request.
        Betas = [AnthropicBeta.ServerSideFallback2026_07_01],
        Fallbacks = new Default(),
        System = new List<BetaTextBlockParam>
        {
            new() { Text = KeywordPrompt.SystemPrompt(vocabulary), CacheControl = new BetaCacheControlEphemeral() },
        },
        OutputConfig = new BetaOutputConfig
        {
            Effort = Effort.Low,
            Format = new BetaJsonOutputFormat { Schema = KeywordPrompt.Schema() },
        },
        Messages = [new BetaUserMessageParam { Content = KeywordPrompt.UserMessage(request) }],
    };
}
