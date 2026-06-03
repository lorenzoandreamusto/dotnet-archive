using DotNetArchive.Cli.Platform;

namespace DotNetArchive.Cli.Services;

public sealed class FileAssociationManager : IFileAssociationManager
{
    private readonly IToolPathResolver _toolPathResolver;
    private readonly TextWriter _stdout;
    private readonly TextWriter _stderr;

    public FileAssociationManager(IToolPathResolver toolPathResolver, TextWriter? stdout = null, TextWriter? stderr = null)
    {
        _toolPathResolver = toolPathResolver;
        _stdout = stdout ?? Console.Out;
        _stderr = stderr ?? Console.Error;
    }

    public int Register()
    {
        IFileAssociation assoc = OperatingSystem.IsWindows()
            ? new WindowsAssociation(_toolPathResolver, _stdout, _stderr)
            : OperatingSystem.IsLinux()
                ? new LinuxAssociation(_toolPathResolver, _stdout, _stderr)
                : OperatingSystem.IsMacOS()
                    ? new MacAssociation(_toolPathResolver, _stdout, _stderr)
                    : throw new PlatformNotSupportedException("The current OS does not support automatic file association.");
        return assoc.Register();
    }

    public int Unregister()
    {
        IFileAssociation assoc = OperatingSystem.IsWindows()
            ? new WindowsAssociation(_toolPathResolver, _stdout, _stderr)
            : OperatingSystem.IsLinux()
                ? new LinuxAssociation(_toolPathResolver, _stdout, _stderr)
                : OperatingSystem.IsMacOS()
                    ? new MacAssociation(_toolPathResolver, _stdout, _stderr)
                    : throw new PlatformNotSupportedException("The current OS does not support automatic file association.");
        return assoc.Unregister();
    }
}
