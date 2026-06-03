using DotNetArchive.Cli.Models;

namespace DotNetArchive.Cli.Services;

public interface IManifestSerializer
{
    string Serialize(Manifest manifest);
    Manifest? Deserialize(string json);
    void WriteToFile(Manifest manifest, string path);
    Manifest? ReadFromFile(string path);
}
