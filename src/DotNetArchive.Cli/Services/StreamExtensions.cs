using System.Diagnostics;

namespace DotNetArchive.Cli.Services;

public static class StreamExtensions
{
    /// <summary>
    /// Runs a process and streams its stdout/stderr line-by-line to the supplied writers.
    /// </summary>
    public static async Task<int> RunAndStreamAsync(
        this ProcessStartInfo startInfo,
        TextWriter stdout,
        TextWriter stderr,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(startInfo);
        ArgumentNullException.ThrowIfNull(stdout);
        ArgumentNullException.ThrowIfNull(stderr);

        startInfo.RedirectStandardOutput = true;
        startInfo.RedirectStandardError = true;
        startInfo.UseShellExecute = false;

        using var process = new Process { StartInfo = startInfo };
        if (!process.Start())
            throw new InvalidOperationException($"Failed to start process: {startInfo.FileName}");

        var stdoutTask = PumpAsync(process.StandardOutput, stdout, cancellationToken);
        var stderrTask = PumpAsync(process.StandardError, stderr, cancellationToken);

        await process.WaitForExitAsync(cancellationToken);
        await Task.WhenAll(stdoutTask, stderrTask);
        return process.ExitCode;
    }

    private static async Task PumpAsync(TextReader reader, TextWriter writer, CancellationToken ct)
    {
        string? line;
        while ((line = await reader.ReadLineAsync(ct)) is not null)
        {
            await writer.WriteLineAsync(line.AsMemory(), ct);
        }
    }
}
