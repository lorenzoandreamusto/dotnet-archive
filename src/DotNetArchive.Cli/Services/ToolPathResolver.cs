using System.Diagnostics;
using System.Runtime.InteropServices;

namespace DotNetArchive.Cli.Services;

public sealed class ToolPathResolver : IToolPathResolver
{
    public string GetCurrentToolPath()
    {
        return Environment.ProcessPath
            ?? throw new InvalidOperationException("Unable to determine the current process path.");
    }

    public string? GetGlobalToolPath()
    {
        try
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = "dotnet",
                Arguments = "tool list -g",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            };
            using var process = Process.Start(startInfo);
            if (process is null) return null;

            string output = process.StandardOutput.ReadToEnd();
            process.WaitForExit();
            if (process.ExitCode != 0) return null;

            foreach (var line in output.Split('\n'))
            {
                var trimmed = line.Trim();
                if (trimmed.StartsWith("dotnet-archive", StringComparison.Ordinal))
                {
                    var parts = trimmed.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length >= 2)
                    {
                        return parts[1];
                    }
                }
            }
            return null;
        }
        catch
        {
            return null;
        }
    }

    public string GetDefaultToolPath()
    {
        string home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        string dotnetDir = Path.Combine(home, ".dotnet");
        string toolsDir = Path.Combine(dotnetDir, "tools");
        string fileName = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
            ? "dotnet-archive.exe"
            : "dotnet-archive";
        return Path.Combine(toolsDir, fileName);
    }

    public string ResolveToolPath()
    {
        try
        {
            var current = GetCurrentToolPath();
            if (File.Exists(current)) return current;
        }
        catch
        {
            // fall through
        }

        var global = GetGlobalToolPath();
        if (global is not null && File.Exists(global)) return global;

        var defaultPath = GetDefaultToolPath();
        if (File.Exists(defaultPath)) return defaultPath;

        throw new FileNotFoundException(
            "Unable to locate the dotnet-archive tool. Tried: current process path, " +
            "`dotnet tool list -g`, and the default ~/.dotnet/tools/ folder.");
    }
}
