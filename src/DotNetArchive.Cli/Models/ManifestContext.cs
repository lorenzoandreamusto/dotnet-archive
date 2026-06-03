using System.Text.Json.Serialization;

namespace DotNetArchive.Cli.Models;

[JsonSourceGenerationOptions(
    WriteIndented = true,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull)]
[JsonSerializable(typeof(Manifest))]
[JsonSerializable(typeof(Dictionary<string, string>))]
internal sealed partial class ManifestContext : JsonSerializerContext
{
}
