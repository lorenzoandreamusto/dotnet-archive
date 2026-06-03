namespace DotNetArchive.Cli.Models;

public sealed class ExportOptions
{
    public required string ProjectPath { get; init; }
    public string? OutputPath { get; init; }
    public string? Runtime { get; init; }
    public bool SelfContained { get; init; }
    public string Compression { get; init; } = "Optimal";
    public List<string> Include { get; init; } = new();
    public List<string> Exclude { get; init; } = new();
    public Dictionary<string, string> ManifestFields { get; init; } = new();
    public TextWriter Stdout { get; init; } = Console.Out;
    public TextWriter Stderr { get; init; } = Console.Error;
}
