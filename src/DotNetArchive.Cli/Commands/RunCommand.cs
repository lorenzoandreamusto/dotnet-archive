using System.CommandLine;
using DotNetArchive.Cli.Models;
using DotNetArchive.Cli.Services;

namespace DotNetArchive.Cli.Commands;

public static class RunCommand
{
    public static Command Create(IArchiveRunner runner)
    {
        ArgumentNullException.ThrowIfNull(runner);

        var archiveArg = new Argument<string>("archive")
        {
            Description = "Path to a .dna archive, a .csproj file, or a project directory (default: current directory).",
            DefaultValueFactory = _ => Directory.GetCurrentDirectory(),
        };
        var appArgsArg = new Argument<string[]>("app-args")
        {
            Description = "Arguments forwarded to the application (use -- to separate from tool flags).",
            DefaultValueFactory = _ => Array.Empty<string>(),
        };
        var noCacheOpt = CommandHelpers.NoCacheOption();
        var envOpt = CommandHelpers.EnvOption();
        var workingDirOpt = CommandHelpers.WorkingDirOption();

        var command = new Command("run", "Execute a packaged .dna archive or compile-and-run a project.");
        command.Arguments.Add(archiveArg);
        command.Arguments.Add(appArgsArg);
        command.Options.Add(noCacheOpt);
        command.Options.Add(envOpt);
        command.Options.Add(workingDirOpt);

        command.SetAction(async parseResult =>
        {
            string archive = parseResult.GetValue(archiveArg) ?? Directory.GetCurrentDirectory();
            string[] appArgs = parseResult.GetValue(appArgsArg) ?? Array.Empty<string>();
            bool noCache = parseResult.GetValue(noCacheOpt);
            var env = CommandHelpers.ParseKeyValuePairs(parseResult.GetValue(envOpt));
            string? workingDir = parseResult.GetValue(workingDirOpt);

            var options = new RunOptions
            {
                ArchivePath = archive,
                AppArgs = appArgs,
                NoCache = noCache,
                Environment = env,
                WorkingDirectory = string.IsNullOrEmpty(workingDir) ? null : workingDir,
            };

            try
            {
                return await runner.RunAsync(options);
            }
            catch (Exception ex)
            {
                await Console.Error.WriteLineAsync($"Error: {ex.Message}");
                return 1;
            }
        });

        return command;
    }
}
