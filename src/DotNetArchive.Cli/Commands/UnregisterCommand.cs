using System.CommandLine;
using DotNetArchive.Cli.Services;

namespace DotNetArchive.Cli.Commands;

public static class UnregisterCommand
{
    public static Command Create(IFileAssociationManager manager)
    {
        ArgumentNullException.ThrowIfNull(manager);
        var command = new Command("unregister", "Remove the .dna file association from this tool.");
        command.SetAction(_ => manager.Unregister());
        return command;
    }
}
