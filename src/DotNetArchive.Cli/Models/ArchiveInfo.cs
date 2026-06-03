namespace DotNetArchive.Cli.Models;

public sealed class ArchiveInfo
{
    public required string FilePath { get; init; }
    public required string Sha256 { get; init; }
    public required long FileSize { get; init; }
    public required long ExtractedSize { get; init; }
    public required int FileCount { get; init; }
    public required Manifest Manifest { get; init; }
    public required bool IsValid { get; init; }
    public string? ValidationError { get; init; }
}
