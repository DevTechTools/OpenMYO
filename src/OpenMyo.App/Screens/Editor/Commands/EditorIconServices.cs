using OpenMyo.App.Screens.Settings;
using OpenMyo.App.Services;
using OpenMyo.Core.Icons;

namespace OpenMyo.App.Screens.Editor.Commands;

/// <summary>What the editor's whole-playlist icon actions need beyond the editor itself.</summary>
/// <param name="Assigner">OpenMyo's own matcher, run in the app.</param>
/// <param name="AiKey">Whether Claude can run here; the same shared key Settings and every picker use.</param>
/// <param name="ClaudeDesktop">Where AI work goes when there's no API key but Claude Desktop is connected.</param>
public sealed record EditorIconServices(IconAutoAssigner Assigner, AiKeyViewModel AiKey, ClaudeDesktopHandoff ClaudeDesktop);
