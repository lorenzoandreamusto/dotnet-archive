using DotNetArchive.Cli.Models;
using DotNetArchive.Cli.Services;
using FluentAssertions;
using Xunit;

namespace DotNetArchive.Tests.Unit;

public class CacheManagerTests : IDisposable
{
    private readonly string _tempCacheDir;

    public CacheManagerTests()
    {
        _tempCacheDir = Path.Combine(Path.GetTempPath(), $"dna-cache-test-{Guid.NewGuid()}");
    }

    public void Dispose()
    {
        try { if (Directory.Exists(_tempCacheDir)) Directory.Delete(_tempCacheDir, recursive: true); } catch { }
    }

    [Fact]
    public void SetOptions_UpdatesCacheDir()
    {
        var cm = new CacheManager();
        cm.SetOptions(new CacheOptions { CacheDir = _tempCacheDir });
        cm.Options.CacheDir.Should().Be(_tempCacheDir);
    }

    [Fact]
    public void GetExtractDir_ReturnsPathUnderCacheDir()
    {
        var cm = new CacheManager();
        cm.SetOptions(new CacheOptions { CacheDir = _tempCacheDir });
        var dir = cm.GetExtractDir("abc123");
        dir.Should().Be(Path.Combine(_tempCacheDir, "abc123"));
    }

    [Fact]
    public void RegisterExtraction_CreatesDirAndMeta()
    {
        var cm = new CacheManager();
        cm.SetOptions(new CacheOptions { CacheDir = _tempCacheDir });
        var hash = "deadbeef" + new string('0', 56);
        cm.RegisterExtraction(hash, "C:/src/app.dna");
        cm.IsExtracted(hash).Should().BeTrue();
        cm.List().Should().ContainSingle(e => e.Hash == hash);
    }

    [Fact]
    public void Invalidate_RemovesDir()
    {
        var cm = new CacheManager();
        cm.SetOptions(new CacheOptions { CacheDir = _tempCacheDir });
        var hash = "feedface" + new string('0', 56);
        cm.RegisterExtraction(hash, "x.dna");
        cm.Invalidate(hash);
        cm.IsExtracted(hash).Should().BeFalse();
    }

    [Fact]
    public void List_ReturnsEmpty_WhenCacheDirDoesNotExist()
    {
        var cm = new CacheManager();
        cm.SetOptions(new CacheOptions { CacheDir = _tempCacheDir });
        cm.List().Should().BeEmpty();
    }

    [Fact]
    public void TouchAccess_DoesNotThrowOnMissingEntry()
    {
        var cm = new CacheManager();
        cm.SetOptions(new CacheOptions { CacheDir = _tempCacheDir });
        var act = () => cm.TouchAccess("nope");
        act.Should().NotThrow();
    }

    [Fact]
    public void Clean_RemovesOldEntries()
    {
        var cm = new CacheManager();
        cm.SetOptions(new CacheOptions { CacheDir = _tempCacheDir, MaxAgeDays = 0 });
        var hash = "0123456789abcdef" + new string('0', 48);
        cm.RegisterExtraction(hash, "x.dna");
        Thread.Sleep(50);
        cm.Clean();
        // With MaxAgeDays=0, the entry should be removed.
        cm.IsExtracted(hash).Should().BeFalse();
    }
}
