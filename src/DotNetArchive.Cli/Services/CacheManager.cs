using System.Text.Json;
using DotNetArchive.Cli.Models;

namespace DotNetArchive.Cli.Services;

public sealed class CacheManager : ICacheManager
{
    private const string MetaFileName = ".dna-meta.json";
    private readonly object _lock = new();

    public CacheOptions Options { get; private set; } = new();

    public void SetOptions(CacheOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        Options = options;
    }

    public string GetExtractDir(string sha256)
    {
        ArgumentException.ThrowIfNullOrEmpty(sha256);
        return Path.Combine(Options.CacheDir, sha256);
    }

    public bool IsExtracted(string sha256)
    {
        return Directory.Exists(GetExtractDir(sha256));
    }

    public void RegisterExtraction(string sha256, string sourcePath)
    {
        ArgumentException.ThrowIfNullOrEmpty(sha256);
        Directory.CreateDirectory(GetExtractDir(sha256));
        var meta = new CacheEntry
        {
            Hash = sha256,
            Path = GetExtractDir(sha256),
            LastAccess = DateTimeOffset.UtcNow,
            SizeBytes = ComputeDirSize(GetExtractDir(sha256)),
            SourcePath = sourcePath,
        };
        WriteMeta(sha256, meta);
    }

    public void TouchAccess(string sha256)
    {
        var meta = ReadMeta(sha256);
        if (meta is null) return;
        meta = new CacheEntry
        {
            Hash = meta.Hash,
            Path = meta.Path,
            LastAccess = DateTimeOffset.UtcNow,
            SizeBytes = meta.SizeBytes,
            SourcePath = meta.SourcePath,
        };
        WriteMeta(sha256, meta);
    }

    public void Invalidate(string sha256)
    {
        var dir = GetExtractDir(sha256);
        if (Directory.Exists(dir))
        {
            Directory.Delete(dir, recursive: true);
        }
    }

    public void Clean()
    {
        if (!Directory.Exists(Options.CacheDir)) return;
        lock (_lock)
        {
            var entries = List().ToList();
            var now = DateTimeOffset.UtcNow;
            var cutoff = now.AddDays(-Options.MaxAgeDays);

            // 1) Remove too-old entries.
            foreach (var entry in entries.Where(e => e.LastAccess < cutoff))
            {
                try { Directory.Delete(entry.Path, recursive: true); }
                catch { /* best effort */ }
            }

            // 2) Enforce total size.
            entries = List().ToList();
            long total = entries.Sum(e => e.SizeBytes);
            foreach (var entry in entries.OrderBy(e => e.LastAccess))
            {
                if (total <= Options.MaxSizeBytes) break;
                try
                {
                    Directory.Delete(entry.Path, recursive: true);
                    total -= entry.SizeBytes;
                }
                catch { /* best effort */ }
            }
        }
    }

    public IEnumerable<CacheEntry> List()
    {
        if (!Directory.Exists(Options.CacheDir)) yield break;
        foreach (var dir in Directory.EnumerateDirectories(Options.CacheDir))
        {
            var meta = ReadMeta(Path.GetFileName(dir));
            if (meta is not null) yield return meta;
        }
    }

    public long TotalSizeBytes()
    {
        return List().Sum(e => e.SizeBytes);
    }

    private CacheEntry? ReadMeta(string sha256)
    {
        var metaPath = Path.Combine(GetExtractDir(sha256), MetaFileName);
        if (!File.Exists(metaPath)) return null;
        try
        {
            var json = File.ReadAllText(metaPath);
            return JsonSerializer.Deserialize<CacheEntry>(json);
        }
        catch
        {
            return null;
        }
    }

    private void WriteMeta(string sha256, CacheEntry entry)
    {
        var metaPath = Path.Combine(GetExtractDir(sha256), MetaFileName);
        var json = JsonSerializer.Serialize(entry, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(metaPath, json);
    }

    private static long ComputeDirSize(string dir)
    {
        long total = 0;
        foreach (var file in Directory.EnumerateFiles(dir, "*", SearchOption.AllDirectories))
        {
            try { total += new FileInfo(file).Length; }
            catch { /* ignore */ }
        }
        return total;
    }
}
