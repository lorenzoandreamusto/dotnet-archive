using System.Diagnostics;
using System.Runtime.InteropServices;
using Microsoft.Win32;

public static class AssociationManager
{
    public static int Register()
    {
        try
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return RegisterWindows();
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                return RegisterLinux();
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                return RegisterMac();
            }
            
            Console.WriteLine("Unsupported operating system for auto-registration.");
            return 1;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error during registration: {ex.Message}");
            return 1;
        }
    }

    private static int RegisterWindows()
    {
        Console.WriteLine("Configuring .dna file association on Windows...");
        
        string userFolder = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        string toolPath = Path.Combine(userFolder, ".dotnet", "tools", "dotnet-archive.exe");

        if (!File.Exists(toolPath))
        {
            Console.WriteLine($"Error: Unable to find the shim at {toolPath}");
            return 1;
        }

        using (var key = Registry.CurrentUser.CreateSubKey(@"Software\Classes\.dna"))
        {
            key.SetValue("", "DotNetArchive.App");
        }
        
        using (var key = Registry.CurrentUser.CreateSubKey(@"Software\Classes\DotNetArchive.App"))
        {
            key.SetValue("", "DotNet Archive Application");
        }
        
        using (var key = Registry.CurrentUser.CreateSubKey(@"Software\Classes\DotNetArchive.App\DefaultIcon"))
        {
            key.SetValue("", $"\"{toolPath}\",0");
        }
        
        using (var key = Registry.CurrentUser.CreateSubKey(@"Software\Classes\DotNetArchive.App\shell\open\command"))
        {
            key.SetValue("", $"\"{toolPath}\" run \"%1\"");
        }

        Console.WriteLine("Registration complete! You can now double-click .dna files.");
        return 0;
    }

    private static int RegisterLinux()
    {
        Console.WriteLine("Configuring .dna file association on Linux...");

        string home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        string mimeDir = Path.Combine(home, ".local", "share", "mime", "packages");
        string appsDir = Path.Combine(home, ".local", "share", "applications");

        Directory.CreateDirectory(mimeDir);
        Directory.CreateDirectory(appsDir);

        string absoluteDotnetDir = Path.Combine(home, ".dotnet");
        string absoluteToolsDir = Path.Combine(absoluteDotnetDir, "tools");
        string absoluteToolPath = Path.Combine(absoluteToolsDir, "dotnet-archive");

        if (!File.Exists(absoluteToolPath))
        {
            Console.WriteLine($"Error: Unable to find the tool at {absoluteToolPath}");
            return 1;
        }

        string mimeXml = @"<?xml version='1.0' encoding='utf-8'?>
    <mime-info xmlns='http://www.freedesktop.org/standards/shared-mime-info'>
    <mime-type type='application/x-dotnet-archive'>
        <comment>.NET Archive Application</comment>
        <glob pattern='*.dna'/>
    </mime-type>
    </mime-info>";
        File.WriteAllText(Path.Combine(mimeDir, "dotnet-archive.xml"), mimeXml);

        string scriptPath = Path.Combine(appsDir, "dotnet-archive-runner.sh");
        string scriptContent = $$"""
    #!/bin/bash
    export PATH="{{absoluteDotnetDir}}:{{absoluteToolsDir}}:$PATH"
    "{{absoluteToolPath}}" run "$1"
    echo ""
    read -p "Premere [Invio] per chiudere questa finestra..." -r
    """;
        File.WriteAllText(scriptPath, scriptContent);
        
        File.SetUnixFileMode(scriptPath, UnixFileMode.UserExecute | UnixFileMode.UserRead | UnixFileMode.UserWrite);

        string desktopEntry = $$"""
    [Desktop Entry]
    Name=DotNet Archive Runner
    Comment=Esegui applicazioni .NET pacchettizzate in .dna
    Exec="{{scriptPath}}" %f
    Icon=system-run
    Terminal=false
    Type=Application
    MimeType=application/x-dotnet-archive;
    Categories=Utility;
    """;
        File.WriteAllText(Path.Combine(appsDir, "dotnet-archive-runner.desktop"), desktopEntry);

        ExecuteCommand("update-mime-database", $"\"{Path.Combine(home, ".local", "share", "mime")}\"");
        ExecuteCommand("update-desktop-database", $"\"{appsDir}\"");
        ExecuteCommand("xdg-mime", "default dotnet-archive-runner.desktop application/x-dotnet-archive");

        Console.WriteLine("Registration completed successfully on Linux!");
        return 0;
    }

    private static int RegisterMac()
    {
        Console.WriteLine("Configuring .dna file association on macOS...");

        string home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        string appFolder = Path.Combine(home, "Applications", "DotNetArchiveRunner.app");
        string contentsDir = Path.Combine(appFolder, "Contents");
        string macOSDir = Path.Combine(contentsDir, "MacOS");

        Directory.CreateDirectory(macOSDir);

        string plistContent = @"<?xml version='1.0' encoding='UTF-8'?>
<!DOCTYPE plist PUBLIC '-//Apple//DTD PLIST 1.0//EN' 'http://www.apple.com/DTDs/PropertyList-1.0.dtd'>
<plist version='1.0'>
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
            <key>CFBundleTypeName</key>
            <string>DotNet Archive File</string>
            <key>CFBundleTypeRole</key>
            <string>Viewer</string>
            <key>LSHandlerRank</key>
            <string>Owner</string>
            <key>LSItemContentTypes</key>
            <array>
                <string>com.user.dna</string>
            </array>
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
</plist>";
        File.WriteAllText(Path.Combine(contentsDir, "Info.plist"), plistContent);

        string runnerScript = $@"#!/bin/bash
export PATH=""$HOME/.dotnet/tools:$PATH""
dotnet-archive run ""$1""
";
        string scriptPath = Path.Combine(macOSDir, "runner");
        File.WriteAllText(scriptPath, runnerScript);

        if (File.Exists(scriptPath))
        {
            File.SetUnixFileMode(scriptPath, UnixFileMode.UserExecute | UnixFileMode.UserRead | UnixFileMode.UserWrite);
        }

        ExecuteCommand("open", $"-g \"{appFolder}\"");

        Console.WriteLine("Registration complete! To finish on macOS, right-click a .dna file, select 'Get Info', set the app 'DotNetArchiveRunner' in 'Open with' and click 'Change All'.");
        return 0;
    }

    private static void ExecuteCommand(string fileName, string arguments)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = fileName,
            Arguments = arguments,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };
        using var process = Process.Start(startInfo);
        process?.WaitForExit();
    }
}