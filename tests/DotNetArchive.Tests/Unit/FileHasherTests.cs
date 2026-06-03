using DotNetArchive.Cli.Services;
using FluentAssertions;
using Xunit;

namespace DotNetArchive.Tests.Unit;

public class FileHasherTests : IDisposable
{
    private readonly string _tempFile;

    public FileHasherTests()
    {
        _tempFile = Path.Combine(Path.GetTempPath(), $"dna-test-{Guid.NewGuid()}.bin");
        File.WriteAllText(_tempFile, "hello world");
    }

    public void Dispose()
    {
        try { if (File.Exists(_tempFile)) File.Delete(_tempFile); } catch { }
    }

    [Fact]
    public void Sha256_ReturnsLowercaseHex()
    {
        var hash = FileHasher.Sha256(_tempFile);
        hash.Should().MatchRegex("^[0-9a-f]{64}$");
    }

    [Fact]
    public void Sha256_IsDeterministic()
    {
        var h1 = FileHasher.Sha256(_tempFile);
        var h2 = FileHasher.Sha256(_tempFile);
        h1.Should().Be(h2);
    }

    [Fact]
    public void Sha256_MatchesReference()
    {
        // SHA-256 of "hello world" is b94d27b9934d3e08a52e52d7da7dabfac484efe37a5380ee9088f7ace2efcde9
        var expected = "b94d27b9934d3e08a52e52d7da7dabfac484efe37a5380ee9088f7ace2efcde9";
        FileHasher.Sha256(_tempFile).Should().Be(expected);
    }

    [Fact]
    public void Sha256_ThrowsOnMissingFile()
    {
        var act = () => FileHasher.Sha256("Z:/no/such/file");
        act.Should().Throw<Exception>();
    }
}
