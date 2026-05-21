using System.Diagnostics;
using System.IO.Compression;
using System.Text.Json;

public static class Exporter
{
    public static int ExportProject(string projectPath, string outputPath)
    {
        string targetPath = string.IsNullOrWhiteSpace(projectPath)
            ? Directory.GetCurrentDirectory()
            : Path.GetFullPath(projectPath);

        string csprojPath = "";

        if (File.Exists(targetPath))
        {
            string extension = Path.GetExtension(targetPath).ToLowerInvariant();
            if (extension == ".csproj")
            {
                csprojPath = targetPath;
            }
            else if (extension == ".sln" || extension == ".slnx")
            {
                Console.WriteLine($"Error: You specified a solution file ({extension}).");
                Console.WriteLine("This tool packages a single executable project (.csproj).");
                Console.WriteLine("Specify the path to the .csproj project file or move to its directory.");
                return 1;
            }
            else
            {
                Console.WriteLine($"Error: The file '{targetPath}' is not a .NET project file (.csproj).");
                return 1;
            }
        }
        else if (Directory.Exists(targetPath))
        {
            var csprojFiles = Directory.GetFiles(targetPath, "*.csproj");

            if (csprojFiles.Length == 1)
            {
                csprojPath = csprojFiles[0];
            }
            else if (csprojFiles.Length > 1)
            {
                Console.WriteLine($"Error: Multiple .csproj files found in the directory '{targetPath}':");
                foreach (var file in csprojFiles)
                {
                    Console.WriteLine($"  - {Path.GetFileName(file)}");
                }
                Console.WriteLine("Explicitly specify which .csproj file to compile and export.");
                return 1;
            }
            else
            {
                var solutionFiles = Directory.GetFiles(targetPath, "*.sln")
                    .Concat(Directory.GetFiles(targetPath, "*.slnx"))
                    .ToArray();

                if (solutionFiles.Length > 0)
                {
                    Console.WriteLine($"Error: No .csproj file found in the current directory, but a solution file is present ({Path.GetFileName(solutionFiles[0])}).");
                    Console.WriteLine("Move to the specific console/executable project directory before running the command.");
                }
                else
                {
                    Console.WriteLine($"Error: No .csproj file found in the directory '{targetPath}'.");
                }
                return 1;
            }
        }
        else
        {
            Console.WriteLine($"Error: The specified path '{targetPath}' does not exist.");
            return 1;
        }

        string projectDir = Path.GetDirectoryName(csprojPath)!;
        string projectName = Path.GetFileNameWithoutExtension(csprojPath);
        string tempPublishDir = Path.Combine(Path.GetTempPath(), $"pub-{Guid.NewGuid()}");

        Console.WriteLine($"Project found: {Path.GetFileName(csprojPath)}");
        Console.WriteLine($"Building and publishing in progress...");

        var startInfo = new ProcessStartInfo
        {
            FileName = "dotnet",
            Arguments = $"publish \"{csprojPath}\" -c Release -o \"{tempPublishDir}\"",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var process = Process.Start(startInfo);
        if (process == null)
        {
            Console.WriteLine("Error: Unable to start the 'dotnet' process.");
            return 1;
        }

        string stdout = process.StandardOutput.ReadToEnd();
        string stderr = process.StandardError.ReadToEnd();

        process.WaitForExit();

        if (process.ExitCode != 0)
        {
            Console.WriteLine("Error during project publishing.");
            if (!string.IsNullOrWhiteSpace(stdout))
            {
                Console.WriteLine("\n--- Build Details ---");
                Console.WriteLine(stdout);
            }
            if (!string.IsNullOrWhiteSpace(stderr))
            {
                Console.WriteLine("\n--- Error Details ---");
                Console.WriteLine(stderr);
            }
            return 1;
        }

        string? mainDll = Directory.GetFiles(tempPublishDir, "*.runtimeconfig.json")
            .Select(f => Path.GetFileName(f).Replace(".runtimeconfig.json", ".dll"))
            .FirstOrDefault(dllName => File.Exists(Path.Combine(tempPublishDir, dllName)));

        if (mainDll == null)
        {
            string fallbackDll = projectName + ".dll";
            if (File.Exists(Path.Combine(tempPublishDir, fallbackDll)))
            {
                mainDll = fallbackDll;
            }
        }

        if (mainDll == null)
        {
            Console.WriteLine("Unable to determine the project entry point (main DLL).");
            return 1;
        }

        var manifest = new { EntryPoint = mainDll };
        string manifestPath = Path.Combine(tempPublishDir, "manifest.json");
        File.WriteAllText(manifestPath, JsonSerializer.Serialize(manifest));

        if (string.IsNullOrEmpty(outputPath))
        {
            outputPath = Path.Combine(Environment.CurrentDirectory, $"{projectName}.dna");
        }

        if (File.Exists(outputPath))
        {
            File.Delete(outputPath);
        }

        ZipFile.CreateFromDirectory(tempPublishDir, outputPath);
        Directory.Delete(tempPublishDir, true);

        Console.WriteLine($"Export completed. Archive created: {outputPath}");
        return 0;
    }
}