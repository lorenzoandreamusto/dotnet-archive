using System.Diagnostics;
using System.IO.Compression;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using DotNetArchive.Cli.Models;

namespace DotNetArchive.Cli.Services;

public sealed class ArchiveExporter : IArchiveExporter
{
    private readonly IManifestSerializer _manifestSerializer;

    public ArchiveExporter(IManifestSerializer manifestSerializer)
    {
        _manifestSerializer = manifestSerializer ?? throw new ArgumentNullException(nameof(manifestSerializer));
    }

    public async Task<string> ExportAsync(ExportOptions options, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(options);

        string csprojPath = ResolveCsproj(options.ProjectPath, options.Stdout, options.Stderr);
        var (projectName, assemblyName, targetFramework) = ReadProjectMetadata(csprojPath);

        string tempPublishDir = Path.Combine(Path.GetTempPath(), $"pub-{Guid.NewGuid()}");
        Directory.CreateDirectory(tempPublishDir);

        try
        {
            await PublishAsync(csprojPath, tempPublishDir, options, cancellationToken);
            ApplyFilters(tempPublishDir, options);
            var manifest = BuildManifest(tempPublishDir, assemblyName, projectName, targetFramework, options);
            _manifestSerializer.WriteToFile(manifest, Path.Combine(tempPublishDir, "manifest.json"));

            string outputPath = ResolveOutputPath(options, projectName);
            if (File.Exists(outputPath)) File.Delete(outputPath);

            var level = ParseCompressionLevel(options.Compression);
            ZipFile.CreateFromDirectory(tempPublishDir, outputPath, level, includeBaseDirectory: false);
            await options.Stdout.WriteLineAsync($"Export completed. Archive created: {outputPath}");
            return outputPath;
        }
        finally
        {
            try { if (Directory.Exists(tempPublishDir)) Directory.Delete(tempPublishDir, recursive: true); }
            catch { /* best effort */ }
        }
    }

    private static string ResolveCsproj(string projectPath, TextWriter stdout, TextWriter stderr)
    {
        string target = string.IsNullOrWhiteSpace(projectPath)
            ? Directory.GetCurrentDirectory()
            : Path.GetFullPath(projectPath);

        if (File.Exists(target))
        {
            string ext = Path.GetExtension(target).ToLowerInvariant();
            return ext switch
            {
                ".csproj" => target,
                ".sln" or ".slnx" => throw new InvalidOperationException(
                    $"Solution files are not supported. Specify a .csproj file or move to its directory."),
                _ => throw new InvalidOperationException($"The file '{target}' is not a .NET project file (.csproj)."),
            };
        }
        if (Directory.Exists(target))
        {
            var csprojFiles = Directory.GetFiles(target, "*.csproj");
            if (csprojFiles.Length == 1) return csprojFiles[0];
            if (csprojFiles.Length > 1) throw new InvalidOperationException(
                $"Multiple .csproj files found in '{target}'. Specify which to compile.");
            throw new InvalidOperationException($"No .csproj file found in '{target}'.");
        }
        throw new InvalidOperationException($"Path '{target}' does not exist.");
    }

    private static (string ProjectName, string AssemblyName, string TargetFramework) ReadProjectMetadata(string csprojPath)
    {
        var doc = XDocument.Load(csprojPath);
        var root = doc.Root ?? throw new InvalidOperationException($"Invalid csproj: {csprojPath}");
        var pg = root.Element("PropertyGroup");
        string assemblyName = pg?.Element("AssemblyName")?.Value
            ?? Path.GetFileNameWithoutExtension(csprojPath);
        string tfm = pg?.Element("TargetFramework")?.Value
            ?? "net8.0";
        return (Path.GetFileNameWithoutExtension(csprojPath), assemblyName, tfm);
    }

    private static async Task PublishAsync(string csprojPath, string publishDir, ExportOptions options, CancellationToken ct)
    {
        bool isSelfContained = options.SelfContained;
        string? rid = options.Runtime;
        if (isSelfContained && string.IsNullOrEmpty(rid))
        {
            rid = RuntimeInformation.RuntimeIdentifier;
        }

        var args = new List<string>
        {
            "publish", csprojPath,
            "-c", "Release",
            "-o", publishDir,
            "-p:PublishSingleFile=false",
            $"-p:UseAppHost={(isSelfContained ? "true" : "false")}",
            "-p:PublishTrimmed=false",
            "-p:PublishReadyToRun=false",
            "-p:PublishAot=false",
        };
        if (!string.IsNullOrEmpty(rid))
        {
            args.Add("-r");
            args.Add(rid);
            args.Add($"-p:SelfContained={(isSelfContained ? "true" : "false")}");
        }
        else
        {
            args.Add("-p:RuntimeIdentifier=");
            args.Add("-p:SelfContained=false");
        }

        var startInfo = new ProcessStartInfo
        {
            FileName = "dotnet",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
        };
        foreach (var a in args) startInfo.ArgumentList.Add(a);

        await options.Stdout.WriteLineAsync($"Building and publishing in progress...");
        var exit = await startInfo.RunAndStreamAsync(options.Stdout, options.Stderr, ct);
        if (exit != 0)
            throw new InvalidOperationException($"dotnet publish exited with code {exit}.");
    }

    private static void ApplyFilters(string publishDir, ExportOptions options)
    {
        if (options.Include.Count == 0 && options.Exclude.Count == 0) return;
        var allFiles = Directory.EnumerateFiles(publishDir, "*", SearchOption.AllDirectories).ToList();
        if (options.Include.Count > 0)
        {
            var includeRegexes = options.Include.Select(p => new Regex(GlobToRegex(p), RegexOptions.IgnoreCase)).ToArray();
            foreach (var file in allFiles)
            {
                var rel = Path.GetRelativePath(publishDir, file).Replace('\\', '/');
                bool keep = includeRegexes.Any(rx => rx.IsMatch(rel));
                if (!keep) TryDelete(file);
            }
            allFiles = Directory.EnumerateFiles(publishDir, "*", SearchOption.AllDirectories).ToList();
        }
        if (options.Exclude.Count > 0)
        {
            var excludeRegexes = options.Exclude.Select(p => new Regex(GlobToRegex(p), RegexOptions.IgnoreCase)).ToArray();
            foreach (var file in allFiles)
            {
                var rel = Path.GetRelativePath(publishDir, file).Replace('\\', '/');
                bool drop = excludeRegexes.Any(rx => rx.IsMatch(rel));
                if (drop) TryDelete(file);
            }
        }
    }

    private static string GlobToRegex(string glob)
    {
        var sb = new System.Text.StringBuilder("^");
        foreach (var c in glob)
        {
            if (c == '*') sb.Append(".*");
            else if (c == '?') sb.Append('.');
            else if ("\\^+.|(){}[]$#".IndexOf(c) >= 0) sb.Append('\\').Append(c);
            else sb.Append(c);
        }
        sb.Append('$');
        return sb.ToString();
    }

    private static void TryDelete(string path)
    {
        try { File.Delete(path); } catch { /* best effort */ }
    }

    private Manifest BuildManifest(string publishDir, string assemblyName, string projectName, string tfm, ExportOptions options)
    {
        var mainDll = $"{assemblyName}.dll";
        string entryPoint = File.Exists(Path.Combine(publishDir, mainDll))
            ? mainDll
            : throw new InvalidOperationException($"Unable to determine the project entry point (expected {mainDll}).");

        string? appHost = null;
        if (options.SelfContained)
        {
            string candidateExe = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
                ? $"{assemblyName}.exe"
                : assemblyName;
            if (File.Exists(Path.Combine(publishDir, candidateExe)))
            {
                appHost = candidateExe;
            }
        }

        var manifest = new Manifest
        {
            EntryPoint = entryPoint,
            ProjectName = projectName,
            Tfm = tfm,
            Rid = options.SelfContained ? (options.Runtime ?? RuntimeInformation.RuntimeIdentifier) : null,
            RuntimeIncluded = options.SelfContained,
            AppHost = appHost,
        };

        // Apply extra fields. Try to map to known properties first; fall back to ExtraFields.
        var known = new Dictionary<string, Action<Manifest, string>>(StringComparer.OrdinalIgnoreCase)
        {
            ["author"] = (m, v) => m.Author = v,
            ["version"] = (m, v) => m.Version = v,
            ["projectName"] = (m, v) => m.ProjectName = v,
            ["tfm"] = (m, v) => m.Tfm = v,
            ["rid"] = (m, v) => m.Rid = v,
        };
        foreach (var kvp in options.ManifestFields)
        {
            if (known.TryGetValue(kvp.Key, out var setter))
            {
                setter(manifest, kvp.Value);
            }
            else
            {
                manifest.ExtraFields[kvp.Key] = kvp.Value;
            }
        }

        return manifest;
    }

    private static string ResolveOutputPath(ExportOptions options, string projectName)
    {
        string output = options.OutputPath ?? Path.Combine(Directory.GetCurrentDirectory(), $"{projectName}.dna");
        if (!output.EndsWith(".dna", StringComparison.OrdinalIgnoreCase))
        {
            output += ".dna";
        }
        return Path.GetFullPath(output);
    }

    private static CompressionLevel ParseCompressionLevel(string s) => s.ToLowerInvariant() switch
    {
        "fastest" => CompressionLevel.Fastest,
        "nocompression" or "none" => CompressionLevel.NoCompression,
        _ => CompressionLevel.Optimal,
    };
}
