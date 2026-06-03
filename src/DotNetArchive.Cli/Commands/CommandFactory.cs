using System.CommandLine;
using DotNetArchive.Cli.Services;

namespace DotNetArchive.Cli.Commands;

public static class CommandFactory
{
    public static RootCommand BuildRoot(
        IArchiveExporter exporter,
        IArchiveRunner runner,
        IFileAssociationManager associations,
        IManifestSerializer manifestSerializer,
        ICacheManager cacheManager)
    {
        var root = new RootCommand($"DotNet Archive Utility — package, run, and inspect .NET applications as portable .dna archives.");

        root.Subcommands.Add(ExportCommand.Create(exporter));
        root.Subcommands.Add(ExportCommand.Create("publish", "Alias of 'export'.", exporter));
        root.Subcommands.Add(RunCommand.Create(runner));
        root.Subcommands.Add(RegisterCommand.Create(associations));
        root.Subcommands.Add(UnregisterCommand.Create(associations));
        root.Subcommands.Add(InfoCommand.Create(manifestSerializer));
        root.Subcommands.Add(VerifyCommand.Create(manifestSerializer));
        root.Subcommands.Add(CacheCommand.Create(cacheManager));
        root.Subcommands.Add(VersionCommand.Create());

        // Default: if user just types `dotnet archive [project]`, route to export.
        root.SetAction(async parseResult =>
        {
            if (parseResult.CommandResult.Command == root)
            {
                // No subcommand matched → run export on the same tokens.
                var exportCmd = ExportCommand.Create(exporter);
                return await exportCmd.Parse(parseResult.Tokens.Select(t => t.Value).ToArray()).InvokeAsync();
            }
            return 0;
        });

        return root;
    }
}
