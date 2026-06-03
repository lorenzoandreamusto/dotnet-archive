using DotNetArchive.Cli.Commands;
using FluentAssertions;
using Xunit;

namespace DotNetArchive.Tests.Unit;

public class CommandHelpersTests
{
    [Fact]
    public void ParseKeyValuePairs_Empty_ReturnsEmpty()
    {
        CommandHelpers.ParseKeyValuePairs(null).Should().BeEmpty();
        CommandHelpers.ParseKeyValuePairs(Array.Empty<string>()).Should().BeEmpty();
    }

    [Fact]
    public void ParseKeyValuePairs_Single()
    {
        var result = CommandHelpers.ParseKeyValuePairs(new[] { "FOO=bar" });
        result.Should().ContainKey("FOO").WhoseValue.Should().Be("bar");
    }

    [Fact]
    public void ParseKeyValuePairs_Multiple()
    {
        var result = CommandHelpers.ParseKeyValuePairs(new[] { "A=1", "B=2" });
        result.Should().HaveCount(2);
        result["A"].Should().Be("1");
        result["B"].Should().Be("2");
    }

    [Fact]
    public void ParseKeyValuePairs_ValueWithEquals()
    {
        var result = CommandHelpers.ParseKeyValuePairs(new[] { "URL=https://x?y=1" });
        result["URL"].Should().Be("https://x?y=1");
    }

    [Theory]
    [InlineData("")]
    [InlineData("=value")]
    [InlineData("noequals")]
    public void ParseKeyValuePairs_RejectsMalformed(string input)
    {
        var act = () => CommandHelpers.ParseKeyValuePairs(new[] { input });
        act.Should().Throw<ArgumentException>();
    }
}
