using System.CommandLine;
using DotNetArchive.Cli.Services;

namespace DotNetArchive.Cli.Commands;

public static class RegisterCommand
{
    public static Command Create(IFileAssociationManager manager)
    {
        ArgumentNullException.ThrowIfNull(manager);
        var command = new Command("register", "Associate the .dna file extension with this tool on the current OS.");
        command.SetAction(_ => manager.Register());
        return command;
    }
}
