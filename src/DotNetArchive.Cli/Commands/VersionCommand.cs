using System.CommandLine;
using DotNetArchive.Cli.Models;

namespace DotNetArchive.Cli.Commands;

public static class VersionCommand
{
    public static Command Create()
    {
        var command = new Command("version", $"Print the {AssemblyInfo.Name} version and exit.");
        command.SetAction(_ =>
        {
            Console.WriteLine($"{AssemblyInfo.Name} {AssemblyInfo.Version}");
            return 0;
        });
        return command;
    }
}
