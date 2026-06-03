using DotNetArchive.Cli.Models;

namespace DotNetArchive.Cli.Services;

public interface IArchiveExporter
{
    /// <summary>
    /// Compiles the .NET project and produces a .dna archive.
    /// Returns the path of the produced archive.
    /// </summary>
    Task<string> ExportAsync(ExportOptions options, CancellationToken cancellationToken = default);
}
