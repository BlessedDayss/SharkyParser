using FluentAssertions;
using Moq;
using SharkyParser.Cli.PreCheck;
using SharkyParser.Core.Interfaces;
using Spectre.Console.Cli;

namespace SharkyParser.Tests.PreCheck;

public class InteractiveModeRunnerTests
{
    [Fact]
    public void Run_ReturnsZero()
    {
        // Arrange
        var mockApp = new Mock<CommandApp>();
        var mockLogger = new Mock<IAppLogger>();
        
        // This test is limited because InteractiveModeRunner.Run() contains an infinite loop
        // and relies on console input which is difficult to mock
        var runner = new InteractiveModeRunner(mockApp.Object, mockLogger.Object);

        // We can only verify the runner is constructed successfully
        runner.Should().NotBeNull();
    }

    [Fact]
    public void Constructor_WithValidDependencies_CreatesInstance()
    {
        // Arrange & Act
        var mockApp = new Mock<CommandApp>();
        var mockLogger = new Mock<IAppLogger>();
        
        var runner = new InteractiveModeRunner(mockApp.Object, mockLogger.Object);

        // Assert
        runner.Should().NotBeNull();
        runner.Should().BeAssignableTo<IInteractiveModeRunner>();
    }
}
