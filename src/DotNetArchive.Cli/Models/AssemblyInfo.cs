using System.Reflection;

namespace DotNetArchive.Cli.Models;

public static class AssemblyInfo
{
    public static string Version { get; } =
        typeof(AssemblyInfo).Assembly
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
        ?? typeof(AssemblyInfo).Assembly.GetName().Version?.ToString()
        ?? "1.0.0";

    public static string Name { get; } = "dotnet-archive";
}
