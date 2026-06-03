namespace DotNetArchive.Cli.Models;

public sealed class CacheOptions
{
    public string CacheDir { get; set; } = Path.Combine(Path.GetTempPath(), "dotnet-archive-cache");
    public int MaxAgeDays { get; set; } = 30;
    public long MaxSizeBytes { get; set; } = 5L * 1024 * 1024 * 1024; // 5 GB
}
