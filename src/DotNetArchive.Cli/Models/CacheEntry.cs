namespace DotNetArchive.Cli.Models;

public sealed class CacheEntry
{
    public required string Hash { get; init; }
    public required string Path { get; init; }
    public required DateTimeOffset LastAccess { get; init; }
    public required long SizeBytes { get; init; }
    public required string SourcePath { get; init; }
}
