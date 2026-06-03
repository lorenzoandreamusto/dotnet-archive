using DotNetArchive.Cli.Models;
using DotNetArchive.Cli.Services;
using FluentAssertions;
using Xunit;

namespace DotNetArchive.Tests.Unit;

public class ManifestSerializerTests
{
    [Fact]
    public void Serialize_Deserialize_RoundTrip()
    {
        var manifest = new Manifest
        {
            EntryPoint = "MyApp.dll",
            ProjectName = "MyApp",
            Tfm = "net8.0",
            Version = "1.2.3",
            Author = "Acme",
            Rid = "linux-x64",
            RuntimeIncluded = true,
            AppHost = "MyApp",
        };
        manifest.ExtraFields["custom"] = "value";

        var serializer = new ManifestSerializer();
        var json = serializer.Serialize(manifest);
        json.Should().Contain("\"EntryPoint\": \"MyApp.dll\"");

        var deserialized = serializer.Deserialize(json);
        deserialized.Should().NotBeNull();
        deserialized!.EntryPoint.Should().Be("MyApp.dll");
        deserialized.ProjectName.Should().Be("MyApp");
        deserialized.RuntimeIncluded.Should().BeTrue();
        deserialized.ExtraFields["custom"].Should().Be("value");
    }

    [Fact]
    public void Deserialize_Throws_ForInvalidJson()
    {
        var serializer = new ManifestSerializer();
        var act = () => serializer.Deserialize("not json");
        act.Should().Throw<System.Text.Json.JsonException>();
    }

    [Fact]
    public void WriteToFile_CreatesFile()
    {
        var path = Path.Combine(Path.GetTempPath(), $"manifest-{Guid.NewGuid()}.json");
        try
        {
            var manifest = new Manifest { EntryPoint = "X.dll" };
            var serializer = new ManifestSerializer();
            serializer.WriteToFile(manifest, path);
            File.Exists(path).Should().BeTrue();
            File.ReadAllText(path).Should().Contain("X.dll");
        }
        finally
        {
            try { if (File.Exists(path)) File.Delete(path); } catch { }
        }
    }

    [Fact]
    public void ReadFromFile_ReturnsNull_ForMissingFile()
    {
        var serializer = new ManifestSerializer();
        serializer.ReadFromFile("Z:/no/such/manifest.json").Should().BeNull();
    }
}
