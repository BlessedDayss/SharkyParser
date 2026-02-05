using FluentAssertions;
using SharkyParser.Cli.PreCheck;

namespace SharkyParser.Tests.PreCheck;

public class CliModeRunnerTests
{
    [Fact]
    public void CliModeRunner_ImplementsInterface()
    {
        // This test verifies that CliModeRunner implements the correct interface
        // Actual functionality testing requires integration tests due to CommandApp being sealed
        typeof(CliModeRunner).Should().Implement<ICliModeRunner>();
    }
}
