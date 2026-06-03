using DotNetArchive.Cli.Services;
using FluentAssertions;
using Xunit;

namespace DotNetArchive.Tests.Unit;

public class ToolPathResolverTests
{
    [Fact]
    public void GetCurrentToolPath_ReturnsValidPath()
    {
        var resolver = new ToolPathResolver();
        var path = resolver.GetCurrentToolPath();
        path.Should().NotBeNullOrEmpty();
        File.Exists(path).Should().BeTrue();
    }

    [Fact]
    public void GetDefaultToolPath_ContainsDotnetArchive()
    {
        var resolver = new ToolPathResolver();
        var path = resolver.GetDefaultToolPath();
        path.Should().Contain("dotnet-archive");
    }
}
