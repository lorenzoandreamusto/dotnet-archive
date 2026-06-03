using System.Diagnostics;
using DotNetArchive.Cli.Services;

namespace DotNetArchive.Cli.Platform;

public sealed class MacAssociation : IFileAssociation
{
    private const string InfoPlistContent = """
<?xml version="1.0" encoding="UTF-8"?>
<!DOCTYPE plist PUBLIC "-//Apple//DTD PLIST 1.0//EN" "http://www.apple.com/DTDs/PropertyList-1.0.dtd">
<plist version="1.0">
<dict>
    <key>CFBundleIdentifier</key>
    <string>com.user.dotnet-archive-runner</string>
    <key>CFBundleName</key>
    <string>DotNet Archive Runner</string>
    <key>CFBundleVersion</key>
    <string>1.0</string>
    <key>CFBundlePackageType</key>
    <string>APPL</string>
    <key>CFBundleExecutable</key>
    <string>runner</string>
    <key>CFBundleDocumentTypes</key>
    <array>
        <dict>
            <key>CFBundleTypeRole</key>
            <string>Viewer</string>
            <key>LSItemContentTypes</key>
            <array>
                <string>com.user.dna</string>
            </array>
            <key>LSHandlerRank</key>
            <string>Owner</string>
        </dict>
    </array>
    <key>UTExportedTypeDeclarations</key>
    <array>
        <dict>
            <key>UTTypeIdentifier</key>
            <string>com.user.dna</string>
            <key>UTTypeConformsTo</key>
            <array>
                <string>public.data</string>
            </array>
            <key>UTTypeDescription</key>
            <string>DotNet Archive</string>
            <key>UTTypeTagSpecification</key>
            <dict>
                <key>public.filename-extension</key>
                <array>
                    <string>dna</string>
                </array>
            </dict>
        </dict>
    </array>
</dict>
</plist>
""";

    private const string RunnerScriptContent = """
#!/bin/bash
export PATH="$HOME/.dotnet/tools:$PATH"
dotnet-archive run "$1"
""";

    private const string AppFolderName = "DotNetArchiveRunner.app";

    private readonly IToolPathResolver _toolPathResolver;
    private readonly TextWriter _stdout;
    private readonly TextWriter _stderr;

    public MacAssociation(IToolPathResolver toolPathResolver, TextWriter? stdout = null, TextWriter? stderr = null)
    {
        _toolPathResolver = toolPathResolver;
        _stdout = stdout ?? Console.Out;
        _stderr = stderr ?? Console.Error;
    }

    public int Register()
    {
        if (!OperatingSystem.IsMacOS())
        {
            _stderr.WriteLine("MacAssociation invoked on a non-macOS OS.");
            return 1;
        }

        try
        {
            _ = _toolPathResolver.ResolveToolPath();

            string home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            string appsRoot = Path.Combine(home, "Applications");
            string appFolder = Path.Combine(appsRoot, AppFolderName);
            string contentsDir = Path.Combine(appFolder, "Contents");
            string macOsDir = Path.Combine(contentsDir, "MacOS");
            Directory.CreateDirectory(macOsDir);

            string plistPath = Path.Combine(contentsDir, "Info.plist");
            File.WriteAllText(plistPath, InfoPlistContent);
            _stdout.WriteLine($"Wrote {plistPath}");

            string runnerPath = Path.Combine(macOsDir, "runner");
            File.WriteAllText(runnerPath, RunnerScriptContent);
            File.SetUnixFileMode(runnerPath,
                UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute |
                UnixFileMode.GroupRead | UnixFileMode.GroupExecute |
                UnixFileMode.OtherRead | UnixFileMode.OtherExecute);
            _stdout.WriteLine($"Wrote {runnerPath}");

            try
            {
                var startInfo = new ProcessStartInfo
                {
                    FileName = "open",
                    Arguments = $"-g \"{appFolder}\"",
                    UseShellExecute = false,
                    CreateNoWindow = true,
                };
                using var process = Process.Start(startInfo);
                process?.WaitForExit();
            }
            catch (Exception ex)
            {
                _stderr.WriteLine($"Warning: failed to notify LaunchServices via 'open': {ex.Message}");
            }

            _stdout.WriteLine("macOS registration complete.");
            return 0;
        }
        catch (Exception ex)
        {
            _stderr.WriteLine($"Error during macOS registration: {ex.Message}");
            return 1;
        }
    }

    public int Unregister()
    {
        if (!OperatingSystem.IsMacOS())
        {
            _stderr.WriteLine("MacAssociation invoked on a non-macOS OS.");
            return 1;
        }

        try
        {
            string home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            string appFolder = Path.Combine(home, "Applications", AppFolderName);
            if (Directory.Exists(appFolder))
            {
                Directory.Delete(appFolder, recursive: true);
            }
            _stdout.WriteLine("macOS unregistration complete.");
            return 0;
        }
        catch (Exception ex)
        {
            _stderr.WriteLine($"Error during macOS unregistration: {ex.Message}");
            return 1;
        }
    }
}
