using System.Diagnostics;
using System.IO.Compression;
using DotNetArchive.Cli.Models;

namespace DotNetArchive.Cli.Services;

public sealed class ArchiveRunner : IArchiveRunner
{
    private readonly IArchiveExporter _exporter;
    private readonly IManifestSerializer _manifestSerializer;
    private readonly ICacheManager _cacheManager;

    public ArchiveRunner(IArchiveExporter exporter, IManifestSerializer manifestSerializer, ICacheManager cacheManager)
    {
        _exporter = exporter ?? throw new ArgumentNullException(nameof(exporter));
        _manifestSerializer = manifestSerializer ?? throw new ArgumentNullException(nameof(manifestSerializer));
        _cacheManager = cacheManager ?? throw new ArgumentNullException(nameof(cacheManager));
    }

    public async Task<int> RunAsync(RunOptions options, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(options);

        // Opportunistic cache GC.
        try { _cacheManager.Clean(); } catch { /* best effort */ }

        string target = string.IsNullOrWhiteSpace(options.ArchivePath)
            ? Directory.GetCurrentDirectory()
            : Path.GetFullPath(options.ArchivePath);

        string ext = Path.GetExtension(target).ToLowerInvariant();
        bool isProject = Directory.Exists(target)
            || (File.Exists(target) && ext != ".dna");

        if (isProject)
        {
            await options.Stdout.WriteLineAsync("Project source detected. Starting compilation and temporary export...");
            string tempDna = Path.Combine(Path.GetTempPath(), $"temp-run-{Guid.NewGuid()}.dna");
            try
            {
                var exportOpts = new ExportOptions
                {
                    ProjectPath = target,
                    OutputPath = tempDna,
                    Stdout = options.Stdout,
                    Stderr = options.Stderr,
                };
                string produced = await _exporter.ExportAsync(exportOpts, cancellationToken);
                return await RunDnaAsync(options, produced, cancellationToken);
            }
            finally
            {
                try { if (File.Exists(tempDna)) File.Delete(tempDna); } catch { /* best effort */ }
            }
        }

        return await RunDnaAsync(options, target, cancellationToken);
    }

    private async Task<int> RunDnaAsync(RunOptions options, string archivePath, CancellationToken cancellationToken)
    {
        if (!File.Exists(archivePath))
            throw new FileNotFoundException($"Archive not found: {archivePath}", archivePath);

        string hash = FileHasher.Sha256(archivePath);
        string extractDir;
        string? tempExtract = null;

        if (options.NoCache)
        {
            tempExtract = Path.Combine(Path.GetTempPath(), $"run-{Guid.NewGuid()}");
            Directory.CreateDirectory(tempExtract);
            ZipFile.ExtractToDirectory(archivePath, tempExtract, overwriteFiles: true);
            extractDir = tempExtract;
        }
        else
        {
            extractDir = _cacheManager.GetExtractDir(hash);
            if (!Directory.Exists(extractDir))
            {
                Directory.CreateDirectory(extractDir);
                ZipFile.ExtractToDirectory(archivePath, extractDir, overwriteFiles: true);
                _cacheManager.RegisterExtraction(hash, archivePath);
            }
            else
            {
                _cacheManager.TouchAccess(hash);
            }
        }

        try
        {
            var manifest = _manifestSerializer.ReadFromFile(Path.Combine(extractDir, "manifest.json"));
            if (manifest is null || string.IsNullOrEmpty(manifest.EntryPoint))
                throw new InvalidOperationException("Invalid archive: missing or invalid manifest.json.");

            string entryPath = Path.Combine(extractDir, manifest.EntryPoint);
            if (!File.Exists(entryPath))
                throw new InvalidOperationException($"Entry point '{manifest.EntryPoint}' not found in archive.");

            // Determine how to launch: apphost (executable) if self-contained + present, else `dotnet <dll>`.
            bool useAppHost = !string.IsNullOrEmpty(manifest.AppHost)
                && File.Exists(Path.Combine(extractDir, manifest.AppHost));

            var startInfo = new ProcessStartInfo
            {
                WorkingDirectory = options.WorkingDirectory ?? extractDir,
                RedirectStandardInput = false,
            };

            if (useAppHost)
            {
                startInfo.FileName = Path.Combine(extractDir, manifest.AppHost!);
                foreach (var a in options.AppArgs) startInfo.ArgumentList.Add(a);
            }
            else
            {
                startInfo.FileName = "dotnet";
                startInfo.ArgumentList.Add(entryPath);
                foreach (var a in options.AppArgs) startInfo.ArgumentList.Add(a);
            }

            // Self-contained: point DOTNET_ROOT at the extracted dir so the bundled runtime is used.
            if (manifest.RuntimeIncluded)
            {
                startInfo.Environment["DOTNET_ROOT"] = extractDir;
            }

            // User-specified env vars.
            foreach (var kvp in options.Environment)
            {
                startInfo.Environment[kvp.Key] = kvp.Value;
            }

            return await startInfo.RunAndStreamAsync(options.Stdout, options.Stderr, cancellationToken);
        }
        finally
        {
            if (tempExtract is not null)
            {
                try { if (Directory.Exists(tempExtract)) Directory.Delete(tempExtract, recursive: true); }
                catch { /* best effort */ }
            }
        }
    }
}
