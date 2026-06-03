using DotNetArchive.Cli.Models;

namespace DotNetArchive.Cli.Services;

public interface IArchiveRunner
{
    /// <summary>
    /// Runs the archive. If the path is a project (.csproj or directory containing one),
    /// exports it on the fly and then runs it. Returns the application's exit code.
    /// </summary>
    Task<int> RunAsync(RunOptions options, CancellationToken cancellationToken = default);
}
