using System.Text.Json;
using DotNetArchive.Cli.Models;

namespace DotNetArchive.Cli.Services;

public sealed class ManifestSerializer : IManifestSerializer
{
    public string Serialize(Manifest manifest)
    {
        ArgumentNullException.ThrowIfNull(manifest);
        return JsonSerializer.Serialize(manifest, ManifestContext.Default.Manifest);
    }

    public Manifest? Deserialize(string json)
    {
        ArgumentException.ThrowIfNullOrEmpty(json);
        return JsonSerializer.Deserialize<Manifest>(json, ManifestContext.Default.Manifest);
    }

    public void WriteToFile(Manifest manifest, string path)
    {
        ArgumentNullException.ThrowIfNull(manifest);
        ArgumentException.ThrowIfNullOrEmpty(path);
        File.WriteAllText(path, Serialize(manifest));
    }

    public Manifest? ReadFromFile(string path)
    {
        ArgumentException.ThrowIfNullOrEmpty(path);
        if (!File.Exists(path)) return null;
        var json = File.ReadAllText(path);
        return Deserialize(json);
    }
}
