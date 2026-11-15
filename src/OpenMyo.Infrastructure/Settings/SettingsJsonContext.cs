using System.Text.Json.Serialization;
using OpenMyo.Core.Abstractions;

namespace OpenMyo.Infrastructure.Settings;

[JsonSourceGenerationOptions(WriteIndented = true)]
[JsonSerializable(typeof(AppSettings))]
internal sealed partial class SettingsJsonContext : JsonSerializerContext;
