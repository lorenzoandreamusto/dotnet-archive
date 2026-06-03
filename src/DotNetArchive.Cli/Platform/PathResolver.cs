using System.Diagnostics;

namespace DotNetArchive.Cli.Platform;

public static class PathResolver
{
    public static bool CommandExists(string command)
    {
        try
        {
            string file = OperatingSystem.IsWindows() ? "where" : "which";
            var startInfo = new ProcessStartInfo
            {
                FileName = file,
                Arguments = command,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            };
            using var process = Process.Start(startInfo);
            if (process is null) return false;
            process.WaitForExit();
            return process.ExitCode == 0;
        }
        catch
        {
            return false;
        }
    }

    public static void TryRunCommand(string command, string args)
    {
        try
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = command,
                Arguments = args,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            };
            using var process = Process.Start(startInfo);
            process?.WaitForExit();
        }
        catch
        {
            // intentionally swallowed: best effort
        }
    }
}
