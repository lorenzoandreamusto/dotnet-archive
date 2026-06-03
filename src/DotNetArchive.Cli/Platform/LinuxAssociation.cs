using DotNetArchive.Cli.Services;

namespace DotNetArchive.Cli.Platform;

public sealed class LinuxAssociation : IFileAssociation
{
    private const string MimePackageContent = """
<?xml version='1.0' encoding='utf-8'?>
<mime-info xmlns='http://www.freedesktop.org/standards/shared-mime-info'>
  <mime-type type='application/x-dotnet-archive'>
    <comment>.NET Archive Application</comment>
    <glob pattern='*.dna'/>
  </mime-type>
</mime-info>
""";

    private const string RunnerScriptTemplate = """
#!/bin/bash
export PATH="$HOME/.dotnet:$PATH"
"{0}" run "$1"
echo ""
read -p "Press [Enter] to close this window..." -r
""";

    private const string DesktopEntryTemplate = """
[Desktop Entry]
Name=DotNet Archive Runner
Comment=Run .NET applications packaged as .dna
Exec="{0}" %f
Icon=system-run
Terminal=false
Type=Application
MimeType=application/x-dotnet-archive;
Categories=Utility;
""";

    private readonly IToolPathResolver _toolPathResolver;
    private readonly TextWriter _stdout;
    private readonly TextWriter _stderr;

    public LinuxAssociation(IToolPathResolver toolPathResolver, TextWriter? stdout = null, TextWriter? stderr = null)
    {
        _toolPathResolver = toolPathResolver;
        _stdout = stdout ?? Console.Out;
        _stderr = stderr ?? Console.Error;
    }

    public int Register()
    {
        if (!OperatingSystem.IsLinux())
        {
            _stderr.WriteLine("LinuxAssociation invoked on a non-Linux OS.");
            return 1;
        }

        string toolPath;
        try
        {
            toolPath = _toolPathResolver.ResolveToolPath();
        }
        catch (Exception ex)
        {
            _stderr.WriteLine($"Failed to resolve tool path: {ex.Message}");
            return 1;
        }

        try
        {
            string home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            string dataHome = Path.Combine(home, ".local", "share");
            string mimeDir = Path.Combine(dataHome, "mime", "packages");
            string appsDir = Path.Combine(dataHome, "applications");
            Directory.CreateDirectory(mimeDir);
            Directory.CreateDirectory(appsDir);

            string mimeFile = Path.Combine(mimeDir, "dotnet-archive.xml");
            File.WriteAllText(mimeFile, MimePackageContent);
            _stdout.WriteLine($"Wrote {mimeFile}");

            string scriptPath = Path.Combine(appsDir, "dotnet-archive-runner.sh");
            File.WriteAllText(scriptPath, string.Format(RunnerScriptTemplate, toolPath));
            File.SetUnixFileMode(scriptPath,
                UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute |
                UnixFileMode.GroupRead | UnixFileMode.GroupExecute |
                UnixFileMode.OtherRead | UnixFileMode.OtherExecute);
            _stdout.WriteLine($"Wrote {scriptPath}");

            string desktopFile = Path.Combine(appsDir, "dotnet-archive-runner.desktop");
            File.WriteAllText(desktopFile, string.Format(DesktopEntryTemplate, scriptPath));
            _stdout.WriteLine($"Wrote {desktopFile}");

            if (PathResolver.CommandExists("update-mime-database"))
            {
                PathResolver.TryRunCommand("update-mime-database", dataHome);
            }
            if (PathResolver.CommandExists("update-desktop-database"))
            {
                PathResolver.TryRunCommand("update-desktop-database", appsDir);
            }
            if (PathResolver.CommandExists("xdg-mime"))
            {
                PathResolver.TryRunCommand("xdg-mime", "default dotnet-archive-runner.desktop application/x-dotnet-archive");
            }

            _stdout.WriteLine("Linux registration complete.");
            return 0;
        }
        catch (Exception ex)
        {
            _stderr.WriteLine($"Error during Linux registration: {ex.Message}");
            return 1;
        }
    }

    public int Unregister()
    {
        if (!OperatingSystem.IsLinux())
        {
            _stderr.WriteLine("LinuxAssociation invoked on a non-Linux OS.");
            return 1;
        }

        try
        {
            string home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            string dataHome = Path.Combine(home, ".local", "share");
            string mimeFile = Path.Combine(dataHome, "mime", "packages", "dotnet-archive.xml");
            string scriptPath = Path.Combine(dataHome, "applications", "dotnet-archive-runner.sh");
            string desktopFile = Path.Combine(dataHome, "applications", "dotnet-archive-runner.desktop");

            if (File.Exists(mimeFile)) File.Delete(mimeFile);
            if (File.Exists(scriptPath)) File.Delete(scriptPath);
            if (File.Exists(desktopFile)) File.Delete(desktopFile);

            if (PathResolver.CommandExists("update-mime-database"))
            {
                PathResolver.TryRunCommand("update-mime-database", dataHome);
            }
            if (PathResolver.CommandExists("update-desktop-database"))
            {
                PathResolver.TryRunCommand("update-desktop-database", Path.Combine(dataHome, "applications"));
            }

            _stdout.WriteLine("Linux unregistration complete.");
            return 0;
        }
        catch (Exception ex)
        {
            _stderr.WriteLine($"Error during Linux unregistration: {ex.Message}");
            return 1;
        }
    }
}
