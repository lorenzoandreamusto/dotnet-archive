using System.IO.Compression;
using System.Runtime.InteropServices;
using DotNetArchive.Cli.Models;
using DotNetArchive.Cli.Services;
using FluentAssertions;
using Xunit;

namespace DotNetArchive.Tests.Integration;

public class AllRidsSmokeTests
{
    public static IEnumerable<object[]> SupportedRids()
    {
        var arch = RuntimeInformation.ProcessArchitecture;
        var os = RuntimeInformation.OSArchitecture;

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            yield return new object[] { "win-x64" };
            if (arch == Architecture.X64 || arch == Architecture.X86)
                yield return new object[] { "win-x86" };
            if (arch == Architecture.Arm64)
                yield return new object[] { "win-arm64" };
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            yield return new object[] { "linux-x64" };
            yield return new object[] { "linux-musl-x64" };
            if (arch == Architecture.Arm64)
            {
                yield return new object[] { "linux-arm64" };
                yield return new object[] { "linux-musl-arm64" };
            }
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            yield return new object[] { "osx-x64" };
            if (arch == Architecture.Arm64)
                yield return new object[] { "osx-arm64" };
        }
    }

    [Theory]
    [MemberData(nameof(SupportedRids))]
    public async Task SelfContained_Export_ProducesValidArchive(string rid)
    {
        var samplePath = Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory, "..", "..", "..", "..", "..", "samples", "HelloWorld.SelfContained", "HelloWorld.SelfContained.csproj"));

        if (!File.Exists(samplePath))
            return; // Sample missing — silently skip (acceptable for CI environments without samples)

        var tempDir = Path.Combine(Path.GetTempPath(), $"dna-rid-{rid}-{Guid.NewGuid()}");
        Directory.CreateDirectory(tempDir);
        try
        {
            // Copy the sample to an isolated temp dir to avoid file-lock races when multiple
            // target frameworks (net8.0, net9.0, net10.0) run this test in parallel.
            var isolatedSample = Path.Combine(tempDir, "HelloWorld.SelfContained");
            CopyDirectory(Path.GetDirectoryName(samplePath)!, isolatedSample);
            var isolatedCsproj = Path.Combine(isolatedSample, "HelloWorld.SelfContained.csproj");

            var outputArchive = Path.Combine(tempDir, $"app-{rid}.dna");
            var serializer = new ManifestSerializer();
            var exporter = new ArchiveExporter(serializer);

            var exported = await exporter.ExportAsync(new ExportOptions
            {
                ProjectPath = isolatedCsproj,
                OutputPath = outputArchive,
                SelfContained = true,
                Runtime = rid,
            });

            File.Exists(exported).Should().BeTrue();
            Path.GetExtension(exported).Should().Be(".dna");

            // Inspect manifest.
            Manifest? manifest = null;
            using (var zip = ZipFile.OpenRead(exported))
            {
                var manifestEntry = zip.GetEntry("manifest.json");
                manifestEntry.Should().NotBeNull();
                using var s = manifestEntry!.Open();
                using var reader = new StreamReader(s);
                var json = await reader.ReadToEndAsync();
                manifest = serializer.Deserialize(json);
            }

            manifest.Should().NotBeNull();
            manifest!.RuntimeIncluded.Should().BeTrue();
            manifest.Rid.Should().Be(rid);
        }
        finally
        {
            try { if (Directory.Exists(tempDir)) Directory.Delete(tempDir, recursive: true); }
            catch { /* best effort */ }
        }
    }

    private static void CopyDirectory(string source, string dest)
    {
        Directory.CreateDirectory(dest);
        foreach (var file in Directory.EnumerateFiles(source, "*", SearchOption.AllDirectories))
        {
            var rel = Path.GetRelativePath(source, file);
            var target = Path.Combine(dest, rel);
            Directory.CreateDirectory(Path.GetDirectoryName(target)!);
            File.Copy(file, target, overwrite: true);
        }
    }
}
