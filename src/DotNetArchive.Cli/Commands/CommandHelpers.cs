using System.CommandLine;

namespace DotNetArchive.Cli.Commands;

internal static class CommandHelpers
{
    /// <summary>
    /// Returns the project Argument used by export. Always defaulted to the current directory.
    /// </summary>
    public static Argument<string> ProjectArgument() =>
        new("project")
        {
            Description = "Path to a .csproj file or a project directory (default: current directory).",
            DefaultValueFactory = _ => Directory.GetCurrentDirectory(),
        };

    public static Option<string> OutputOption(char shortName = 'o', string name = "--output") =>
        new(name, new[] { $"-{shortName}" })
        {
            Description = "Destination path for the .dna output file.",
        };

    public static Option<string> RuntimeOption() =>
        new("--runtime", "-r")
        {
            Description = "Target runtime identifier (e.g. linux-x64, win-x64, osx-arm64).",
        };

    public static Option<bool> SelfContainedOption() =>
        new("--self-contained")
        {
            Description = "Bundle a portable .NET runtime into the archive (increases size).",
        };

    public static Option<string> CompressionOption() =>
        new("--compression")
        {
            Description = "ZIP compression level: Optimal | Fastest | NoCompression (default Optimal).",
            DefaultValueFactory = _ => "Optimal",
        };

    public static Option<string[]> IncludeOption() =>
        new("--include")
        {
            Description = "Glob pattern(s) of files to include (can be repeated). If unset, all files are included.",
            AllowMultipleArgumentsPerToken = true,
        };

    public static Option<string[]> ExcludeOption() =>
        new("--exclude")
        {
            Description = "Glob pattern(s) of files to exclude (can be repeated).",
            AllowMultipleArgumentsPerToken = true,
        };

    public static Option<string[]> ManifestFieldOption() =>
        new("--manifest-field")
        {
            Description = "Add a custom field to the manifest, as KEY=VALUE. Repeatable.",
            AllowMultipleArgumentsPerToken = true,
        };

    public static Option<bool> NoCacheOption() =>
        new("--no-cache")
        {
            Description = "Extract the archive to a temporary directory and clean it up afterwards.",
        };

    public static Option<string[]> EnvOption() =>
        new("--env")
        {
            Description = "Environment variable to pass to the application, as KEY=VALUE. Repeatable.",
            AllowMultipleArgumentsPerToken = true,
        };

    public static Option<string?> WorkingDirOption() =>
        new("--working-dir")
        {
            Description = "Working directory for the spawned process (default: the archive's extraction dir).",
        };

    /// <summary>Parses strings of the form KEY=VALUE. Throws on malformed entries.</summary>
    public static Dictionary<string, string> ParseKeyValuePairs(string[]? items)
    {
        var dict = new Dictionary<string, string>(StringComparer.Ordinal);
        if (items is null) return dict;
        foreach (var item in items)
        {
            int eq = item.IndexOf('=');
            if (eq <= 0 || eq == item.Length - 1)
                throw new ArgumentException($"Expected KEY=VALUE format, got '{item}'.");
            dict[item[..eq]] = item[(eq + 1)..];
        }
        return dict;
    }
}
