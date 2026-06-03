using DotNetArchive.Cli.Models;

namespace DotNetArchive.Cli.Services;

public interface ICacheManager
{
    CacheOptions Options { get; }
    void SetOptions(CacheOptions options);
    string GetExtractDir(string sha256);
    bool IsExtracted(string sha256);
    void RegisterExtraction(string sha256, string sourcePath);
    void TouchAccess(string sha256);
    void Invalidate(string sha256);
    void Clean();
    IEnumerable<CacheEntry> List();
    long TotalSizeBytes();
}
