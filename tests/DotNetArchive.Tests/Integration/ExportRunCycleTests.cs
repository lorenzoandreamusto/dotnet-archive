using System.IO.Compression;
using DotNetArchive.Cli.Models;
using DotNetArchive.Cli.Services;
using FluentAssertions;
using Xunit;

namespace DotNetArchive.Tests.Integration;

public class ExportRunCycleTests : IDisposable
{
    private readonly string _tempDir;
    private readonly string _outputArchive;

    private static readonly string SamplePath = Path.GetFullPath(Path.Combine(
        AppContext.BaseDirectory, "..", "..", "..", "..", "..", "samples", "HelloWorld", "HelloWorld.csproj"));

    public ExportRunCycleTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"dna-integ-{Guid.NewGuid()}");
        Directory.CreateDirectory(_tempDir);
        _outputArchive = Path.Combine(_tempDir, "HelloWorld.dna");
    }

    public void Dispose()
    {
        try { if (Directory.Exists(_tempDir)) Directory.Delete(_tempDir, recursive: true); }
        catch { /* best effort */ }
    }

    [Fact]
    public async Task Export_ProducesValidArchive()
    {
        File.Exists(SamplePath).Should().BeTrue($"sample should exist at {SamplePath}");

        var exporter = new ArchiveExporter(new ManifestSerializer());
        var result = await exporter.ExportAsync(new ExportOptions
        {
            ProjectPath = SamplePath,
            OutputPath = _outputArchive,
            Compression = "Optimal",
        });
        result.Should().Be(_outputArchive);
        File.Exists(_outputArchive).Should().BeTrue();
        Path.GetExtension(_outputArchive).Should().Be(".dna");
    }

    [Fact]
    public async Task Archive_ContainsManifestJson()
    {
        File.Exists(SamplePath).Should().BeTrue($"sample should exist at {SamplePath}");

        var exporter = new ArchiveExporter(new ManifestSerializer());
        await exporter.ExportAsync(new ExportOptions
        {
            ProjectPath = SamplePath,
            OutputPath = _outputArchive,
        });

        using var zip = ZipFile.OpenRead(_outputArchive);
        zip.GetEntry("manifest.json").Should().NotBeNull();
    }

    [Fact]
    public async Task Run_NoCache_ForwardsArgs()
    {
        File.Exists(SamplePath).Should().BeTrue($"sample should exist at {SamplePath}");

        var serializer = new ManifestSerializer();
        var exporter = new ArchiveExporter(serializer);
        var cache = new CacheManager();
        cache.SetOptions(new CacheOptions { CacheDir = Path.Combine(_tempDir, "cache") });
        var runner = new ArchiveRunner(exporter, serializer, cache);

        await exporter.ExportAsync(new ExportOptions
        {
            ProjectPath = SamplePath,
            OutputPath = _outputArchive,
        });

        var stdout = new StringWriter();
        var stderr = new StringWriter();
        var exitCode = await runner.RunAsync(new RunOptions
        {
            ArchivePath = _outputArchive,
            AppArgs = new[] { "--integ" },
            NoCache = true,
            Stdout = stdout,
            Stderr = stderr,
        });

        exitCode.Should().Be(0);
        stdout.ToString().Should().Contain("Hello, dotnet-archive!");
        stdout.ToString().Should().Contain("--integ");
    }

    [Fact]
    public async Task Run_ReusesCache()
    {
        File.Exists(SamplePath).Should().BeTrue($"sample should exist at {SamplePath}");

        var serializer = new ManifestSerializer();
        var exporter = new ArchiveExporter(serializer);
        var cacheDir = Path.Combine(_tempDir, "cache");
        var cache = new CacheManager();
        cache.SetOptions(new CacheOptions { CacheDir = cacheDir });
        var runner = new ArchiveRunner(exporter, serializer, cache);

        await exporter.ExportAsync(new ExportOptions
        {
            ProjectPath = SamplePath,
            OutputPath = _outputArchive,
        });

        await runner.RunAsync(new RunOptions { ArchivePath = _outputArchive, NoCache = false });
        Directory.Exists(cacheDir).Should().BeTrue();
        var entriesAfterFirst = Directory.GetDirectories(cacheDir).Length;
        entriesAfterFirst.Should().BeGreaterThan(0);

        await runner.RunAsync(new RunOptions { ArchivePath = _outputArchive, NoCache = false });
        Directory.GetDirectories(cacheDir).Length.Should().Be(entriesAfterFirst);
    }
}
