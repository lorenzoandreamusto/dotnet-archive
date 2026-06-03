namespace DotNetArchive.Cli.Models;

public sealed class RunOptions
{
    public required string ArchivePath { get; init; }
    public string[] AppArgs { get; init; } = Array.Empty<string>();
    public bool NoCache { get; init; }
    public Dictionary<string, string> Environment { get; init; } = new();
    public string? WorkingDirectory { get; init; }
    public TextWriter Stdout { get; init; } = Console.Out;
    public TextWriter Stderr { get; init; } = Console.Error;
}
