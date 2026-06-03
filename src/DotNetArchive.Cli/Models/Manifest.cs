namespace DotNetArchive.Cli.Models;

public sealed class Manifest
{
    public string EntryPoint { get; set; } = string.Empty;
    public string? ProjectName { get; set; }
    public string? Tfm { get; set; }
    public string? Version { get; set; }
    public string? Author { get; set; }
    public string? Rid { get; set; }
    public bool RuntimeIncluded { get; set; }
    public string? AppHost { get; set; }
    public Dictionary<string, string> ExtraFields { get; set; } = new();
}
