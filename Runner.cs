using System.Diagnostics;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

public static class Runner
{
    public static int RunArchive(string archivePath, string[] appArgs)
    {
        string targetPath = string.IsNullOrWhiteSpace(archivePath)
            ? Directory.GetCurrentDirectory()
            : Path.GetFullPath(archivePath);

        string extension = Path.GetExtension(targetPath).ToLowerInvariant();

        if (Directory.Exists(targetPath) || (File.Exists(targetPath) && extension != ".dna"))
        {
            Console.WriteLine("Project source detected. Starting compilation and temporary export...");
            
            string tempdnaPath = Path.Combine(Path.GetTempPath(), $"temp-run-{Guid.NewGuid()}.dna");

            int exportResult = Exporter.ExportProject(targetPath, tempdnaPath);
            if (exportResult != 0)
            {
                return exportResult;
            }

            int runResult = RunArchiveInternal(tempdnaPath, appArgs);

            try
            {
                if (File.Exists(tempdnaPath))
                {
                    File.Delete(tempdnaPath);
                }
            }
            catch 
            {
            }

            return runResult;
        }
        else
        {
            return RunArchiveInternal(targetPath, appArgs);
        }
    }

    private static int RunArchiveInternal(string archivePath, string[] appArgs)
    {
        if (!File.Exists(archivePath))
        {
            Console.WriteLine($"Error: the file {archivePath} does not exist.");
            return 1;
        }

        string fileHash = GetFileHash(archivePath);
        
        string cacheBaseDir = Path.Combine(Path.GetTempPath(), "dotnet-archive-cache");
        string extractDir = Path.Combine(cacheBaseDir, fileHash);

        if (!Directory.Exists(extractDir))
        {
            Directory.CreateDirectory(extractDir);
            ZipFile.ExtractToDirectory(archivePath, extractDir);
        }

        string manifestPath = Path.Combine(extractDir, "manifest.json");
        if (!File.Exists(manifestPath))
        {
            Console.WriteLine("Error: Invalid archive. Missing manifest.json file.");
            return 1;
        }

        var json = File.ReadAllText(manifestPath);
        var manifest = JsonSerializer.Deserialize<Dictionary<string, string>>(json);
        
        if (manifest == null || !manifest.TryGetValue("EntryPoint", out var entryPoint))
        {
            Console.WriteLine("Error: Invalid manifest.json or missing EntryPoint.");
            return 1;
        }

        string dllPath = Path.Combine(extractDir, entryPoint);
        if (!File.Exists(dllPath))
        {
            Console.WriteLine($"Error: the specified entry point ({entryPoint}) was not found in the archive.");
            return 1;
        }

        var arguments = new List<string> { $"\"{dllPath}\"" };
        arguments.AddRange(appArgs);

        var startInfo = new ProcessStartInfo
        {
            FileName = "dotnet",
            Arguments = string.Join(" ", arguments.Select(a => a.Contains(" ") && !a.StartsWith("\"") ? $"\"{a}\"" : a)),
            UseShellExecute = false,
            CreateNoWindow = false
        };

        using var process = Process.Start(startInfo);
        process?.WaitForExit();
        return process?.ExitCode ?? 0;
    }

    private static string GetFileHash(string filePath)
    {
        using var sha256 = SHA256.Create();
        using var stream = File.OpenRead(filePath);
        var hash = sha256.ComputeHash(stream);
        var sb = new StringBuilder();
        foreach (var b in hash)
        {
            sb.Append(b.ToString("x2"));
        }
        return sb.ToString();
    }
}