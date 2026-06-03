using System.CommandLine;
using System.IO.Compression;
using System.Text;
using DotNetArchive.Cli.Models;
using DotNetArchive.Cli.Services;

namespace DotNetArchive.Cli.Commands;

public static class InfoCommand
{
    public static Command Create(IManifestSerializer serializer)
    {
        ArgumentNullException.ThrowIfNull(serializer);

        var archiveArg = new Argument<string>("archive")
        {
            Description = "Path to the .dna archive to inspect.",
        };
        var jsonOpt = new Option<bool>("--json") { Description = "Emit JSON instead of human-readable text." };

        var command = new Command("info", "Show manifest and contents of a .dna archive.");
        command.Arguments.Add(archiveArg);
        command.Options.Add(jsonOpt);

        command.SetAction(parseResult =>
        {
            string archive = parseResult.GetValue(archiveArg)!;
            if (!File.Exists(archive)) { Console.Error.WriteLine($"Error: file not found: {archive}"); return 1; }
            bool json = parseResult.GetValue(jsonOpt);

            var info = Inspect(archive, serializer);
            if (json) EmitJson(info);
            else EmitHuman(info);
            return info.IsValid ? 0 : 1;
        });

        return command;
    }

    public static ArchiveInfo Inspect(string archivePath, IManifestSerializer serializer)
    {
        if (!File.Exists(archivePath))
            return new ArchiveInfo
            {
                FilePath = archivePath,
                Sha256 = string.Empty,
                FileSize = 0,
                ExtractedSize = 0,
                FileCount = 0,
                Manifest = new Manifest(),
                IsValid = false,
                ValidationError = "File not found.",
            };

        var fi = new FileInfo(archivePath);
        string hash = FileHasher.Sha256(archivePath);

        long extractedSize = 0;
        int fileCount = 0;
        Manifest? manifest = null;
        string? error = null;

        try
        {
            using var zip = ZipFile.OpenRead(archivePath);
            foreach (var entry in zip.Entries)
            {
                extractedSize += entry.Length;
                fileCount++;
                if (string.Equals(entry.FullName, "manifest.json", StringComparison.OrdinalIgnoreCase))
                {
                    using var s = entry.Open();
                    using var reader = new StreamReader(s, Encoding.UTF8);
                    var json = reader.ReadToEnd();
                    manifest = serializer.Deserialize(json);
                }
            }
        }
        catch (Exception ex)
        {
            error = $"Failed to read archive: {ex.Message}";
        }

        bool valid = manifest is not null && !string.IsNullOrEmpty(manifest.EntryPoint) && error is null;
        return new ArchiveInfo
        {
            FilePath = archivePath,
            Sha256 = hash,
            FileSize = fi.Length,
            ExtractedSize = extractedSize,
            FileCount = fileCount,
            Manifest = manifest ?? new Manifest(),
            IsValid = valid,
            ValidationError = error ?? (valid ? null : "Manifest missing or entry point empty."),
        };
    }

    private static void EmitHuman(ArchiveInfo info)
    {
        var w = new StringBuilder();
        w.AppendLine($"Archive:    {info.FilePath}");
        w.AppendLine($"SHA-256:    {info.Sha256}");
        w.AppendLine($"File size:  {FormatBytes(info.FileSize)}");
        w.AppendLine($"Extracted:  {FormatBytes(info.ExtractedSize)}  ({info.FileCount} files)");
        w.AppendLine($"Valid:      {(info.IsValid ? "yes" : "no" + (info.ValidationError is null ? "" : $" ({info.ValidationError})"))}");
        w.AppendLine();
        w.AppendLine("Manifest:");
        w.AppendLine($"  EntryPoint:     {info.Manifest.EntryPoint}");
        if (info.Manifest.ProjectName is not null) w.AppendLine($"  ProjectName:    {info.Manifest.ProjectName}");
        if (info.Manifest.Tfm is not null) w.AppendLine($"  Tfm:            {info.Manifest.Tfm}");
        if (info.Manifest.Version is not null) w.AppendLine($"  Version:        {info.Manifest.Version}");
        if (info.Manifest.Author is not null) w.AppendLine($"  Author:         {info.Manifest.Author}");
        if (info.Manifest.Rid is not null) w.AppendLine($"  Rid:            {info.Manifest.Rid}");
        w.AppendLine($"  RuntimeIncluded: {info.Manifest.RuntimeIncluded}");
        if (info.Manifest.AppHost is not null) w.AppendLine($"  AppHost:        {info.Manifest.AppHost}");
        foreach (var kvp in info.Manifest.ExtraFields)
            w.AppendLine($"  {kvp.Key}:{new string(' ', Math.Max(0, 16 - kvp.Key.Length))} {kvp.Value}");
        Console.Write(w.ToString());
    }

    private static void EmitJson(ArchiveInfo info)
    {
        var obj = new
        {
            filePath = info.FilePath,
            sha256 = info.Sha256,
            fileSize = info.FileSize,
            extractedSize = info.ExtractedSize,
            fileCount = info.FileCount,
            isValid = info.IsValid,
            validationError = info.ValidationError,
            manifest = info.Manifest,
        };
        Console.WriteLine(System.Text.Json.JsonSerializer.Serialize(obj, new System.Text.Json.JsonSerializerOptions { WriteIndented = true }));
    }

    private static string FormatBytes(long bytes)
    {
        string[] units = { "B", "KB", "MB", "GB" };
        double v = bytes;
        int u = 0;
        while (v >= 1024 && u < units.Length - 1) { v /= 1024; u++; }
        return $"{v:0.##} {units[u]}";
    }
}
