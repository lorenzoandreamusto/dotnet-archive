using System.CommandLine;
using DotNetArchive.Cli.Services;

namespace DotNetArchive.Cli.Commands;

public static class VerifyCommand
{
    public static Command Create(IManifestSerializer serializer)
    {
        ArgumentNullException.ThrowIfNull(serializer);

        var archiveArg = new Argument<string>("archive")
        {
            Description = "Path to the .dna archive to verify.",
        };

        var command = new Command("verify", "Verify that a .dna archive is intact and has a valid manifest.");
        command.Arguments.Add(archiveArg);

        command.SetAction(parseResult =>
        {
            string archive = parseResult.GetValue(archiveArg)!;
            if (!File.Exists(archive)) { Console.Error.WriteLine($"Error: file not found: {archive}"); return 1; }
            var info = InfoCommand.Inspect(archive, serializer);
            if (info.IsValid)
            {
                Console.WriteLine($"OK: {archive} is a valid .dna archive.");
                Console.WriteLine($"  Entry point: {info.Manifest.EntryPoint}");
                Console.WriteLine($"  SHA-256:     {info.Sha256}");
                return 0;
            }
            else
            {
                Console.Error.WriteLine($"FAIL: {info.ValidationError ?? "Invalid archive."}");
                return 1;
            }
        });

        return command;
    }
}
