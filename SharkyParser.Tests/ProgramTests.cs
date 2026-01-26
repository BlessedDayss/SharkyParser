using FluentAssertions;
using SharkyParser.Cli;

namespace SharkyParser.Tests;

[Collection("Console")]
public class ProgramTests
{
    [Fact]
    public void Main_WithHelp_ReturnsSuccess()
    {
        var exitCode = Program.Main(["--help"]);

        exitCode.Should().Be(0);
    }
}
