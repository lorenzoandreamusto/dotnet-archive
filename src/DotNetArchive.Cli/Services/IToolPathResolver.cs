namespace DotNetArchive.Cli.Services;

public interface IToolPathResolver
{
    /// <summary>
    /// Returns the full path to the currently running dotnet-archive executable.
    /// </summary>
    string GetCurrentToolPath();

    /// <summary>
    /// Returns the path to dotnet-archive as a global tool, by querying `dotnet tool list -g`.
    /// Returns null if not installed as a global tool.
    /// </summary>
    string? GetGlobalToolPath();

    /// <summary>
    /// Returns the expected default path for the tool in the standard .NET tools folder.
    /// </summary>
    string GetDefaultToolPath();

    /// <summary>
    /// Tries the three strategies in order: current process path, global tool path, default path.
    /// Throws if none is found.
    /// </summary>
    string ResolveToolPath();
}
