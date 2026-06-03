using System.CommandLine;
using DotNetArchive.Cli.Models;
using DotNetArchive.Cli.Services;

namespace DotNetArchive.Cli.Commands;

public static class ExportCommand
{
    public static Command Create(IArchiveExporter exporter) => Create("export", "Compile a .NET project and pack it into a .dna archive.", exporter);

    public static Command Create(string name, string description, IArchiveExporter exporter)
    {
        ArgumentNullException.ThrowIfNull(exporter);

        var projectArg = CommandHelpers.ProjectArgument();
        var outputOpt = CommandHelpers.OutputOption();
        var runtimeOpt = CommandHelpers.RuntimeOption();
        var selfContainedOpt = CommandHelpers.SelfContainedOption();
        var compressionOpt = CommandHelpers.CompressionOption();
        var includeOpt = CommandHelpers.IncludeOption();
        var excludeOpt = CommandHelpers.ExcludeOption();
        var fieldOpt = CommandHelpers.ManifestFieldOption();

        var command = new Command(name, description);
        command.Arguments.Add(projectArg);
        command.Options.Add(outputOpt);
        command.Options.Add(runtimeOpt);
        command.Options.Add(selfContainedOpt);
        command.Options.Add(compressionOpt);
        command.Options.Add(includeOpt);
        command.Options.Add(excludeOpt);
        command.Options.Add(fieldOpt);

        command.SetAction(async parseResult =>
        {
            string project = parseResult.GetValue(projectArg) ?? Directory.GetCurrentDirectory();
            string? output = parseResult.GetValue(outputOpt);
            string? runtime = parseResult.GetValue(runtimeOpt);
            bool selfContained = parseResult.GetValue(selfContainedOpt);
            string compression = parseResult.GetValue(compressionOpt) ?? "Optimal";
            var include = parseResult.GetValue(includeOpt) ?? Array.Empty<string>();
            var exclude = parseResult.GetValue(excludeOpt) ?? Array.Empty<string>();
            var fields = CommandHelpers.ParseKeyValuePairs(parseResult.GetValue(fieldOpt));

            var options = new ExportOptions
            {
                ProjectPath = project,
                OutputPath = string.IsNullOrEmpty(output) ? null : output,
                Runtime = string.IsNullOrEmpty(runtime) ? null : runtime,
                SelfContained = selfContained,
                Compression = compression,
                Include = include.ToList(),
                Exclude = exclude.ToList(),
                ManifestFields = fields,
            };

            try
            {
                string archive = await exporter.ExportAsync(options);
                return 0;
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
