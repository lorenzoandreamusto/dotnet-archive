using DotNetArchive.Cli.Models;
using FluentAssertions;
using Xunit;

namespace DotNetArchive.Tests.Unit;

public class AssemblyInfoTests
{
    [Fact]
    public void Name_IsDotnetArchive()
    {
        AssemblyInfo.Name.Should().Be("dotnet-archive");
    }

    [Fact]
    public void Version_IsNotEmpty()
    {
        AssemblyInfo.Version.Should().NotBeNullOrEmpty();
    }
}
