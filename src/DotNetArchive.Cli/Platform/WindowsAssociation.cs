using DotNetArchive.Cli.Services;
using Microsoft.Win32;

namespace DotNetArchive.Cli.Platform;

public sealed class WindowsAssociation : IFileAssociation
{
    private readonly IToolPathResolver _toolPathResolver;
    private readonly TextWriter _stdout;
    private readonly TextWriter _stderr;

    public WindowsAssociation(IToolPathResolver toolPathResolver, TextWriter? stdout = null, TextWriter? stderr = null)
    {
        _toolPathResolver = toolPathResolver;
        _stdout = stdout ?? Console.Out;
        _stderr = stderr ?? Console.Error;
    }

    public int Register()
    {
        if (!OperatingSystem.IsWindows())
        {
            _stderr.WriteLine("WindowsAssociation invoked on a non-Windows OS.");
            return 1;
        }

        try
        {
            _stdout.WriteLine("Configuring .dna file association on Windows...");
            string toolPath = _toolPathResolver.ResolveToolPath();

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

            _stdout.WriteLine("Registration complete! You can now double-click .dna files.");
            return 0;
        }
        catch (Exception ex)
        {
            _stderr.WriteLine($"Error during Windows registration: {ex.Message}");
            return 1;
        }
    }

    public int Unregister()
    {
        if (!OperatingSystem.IsWindows())
        {
            _stderr.WriteLine("WindowsAssociation invoked on a non-Windows OS.");
            return 1;
        }

        try
        {
            Registry.CurrentUser.DeleteSubKeyTree(@"Software\Classes\DotNetArchive.App", throwOnMissingSubKey: false);
            Registry.CurrentUser.DeleteSubKeyTree(@"Software\Classes\.dna", throwOnMissingSubKey: false);
            _stdout.WriteLine("Unregistration complete.");
            return 0;
        }
        catch (Exception ex)
        {
            _stderr.WriteLine($"Error during unregistration: {ex.Message}");
            return 1;
        }
    }
}
