using FluentAssertions;
using SharkyParser.Cli.PreCheck;

namespace SharkyParser.Tests.PreCheck;

public class EmbeddedModeRunnerTests
{
    [Fact]
    public void EmbeddedModeRunner_ImplementsInterface()
    {
        // This test verifies that EmbeddedModeRunner implements the correct interface
        // Actual functionality testing requires integration tests due to CommandApp being sealed
        typeof(EmbeddedModeRunner).Should().Implement<IEmbeddedModeRunner>();
    }
}
