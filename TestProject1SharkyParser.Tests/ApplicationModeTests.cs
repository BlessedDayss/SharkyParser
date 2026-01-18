using FluentAssertions;
using Moq;
using SharkyParser.Cli;
using SharkyParser.Cli.PreCheck;
using SharkyParser.Core.Interfaces;

namespace SharkyParser.Tests;

public class ApplicationModeTests
{
    private readonly ApplicationModeDetector _detector = new();
    private readonly Mock<IAppLogger> _mockLogger = new();
    private readonly Mock<ICliModeRunner> _mockCliRunner = new();
    private readonly Mock<IInteractiveModeRunner> _mockInteractiveRunner = new();
    private readonly Mock<IEmbeddedModeRunner> _mockEmbeddedRunner = new();

    [Fact]
    public void DetermineMode_WithEmbeddedFlag_ReturnsEmbedded()
    {
        string[] args = ["parse", "file.log", "--embedded"];
        _detector.DetermineMode(args).Should().Be(ApplicationMode.Embedded);
    }

    [Fact]
    public void DetermineMode_WithEmbeddedFlagMixedCase_ReturnsEmbedded()
    {
        string[] args = ["--EmBeDdEd"];
        _detector.DetermineMode(args).Should().Be(ApplicationMode.Embedded);
    }

    [Fact]
    public void DetermineMode_WithArgumentsAndNoEmbeddedFlag_ReturnsCli()
    {
        string[] args = ["analyze", "path/to/log"];
        _detector.DetermineMode(args).Should().Be(ApplicationMode.Cli);
    }

    [Fact]
    public void DetermineMode_WithNoArguments_ReturnsInteractive()
    {
        string[] args = [];
        _detector.DetermineMode(args).Should().Be(ApplicationMode.Interactive);
    }

    [Fact]
    public void ApplicationRunner_ShouldCallEmbeddedRunner_WhenModeIsEmbedded()
    {
        // Arrange
        var args = new[] { "parse", "log.txt", "--embedded" };
        var runner = new ApplicationRunner(_detector, _mockCliRunner.Object, _mockInteractiveRunner.Object, _mockEmbeddedRunner.Object, _mockLogger.Object);
        _mockEmbeddedRunner.Setup(r => r.Run(args)).Returns(0);

        // Act
        var result = runner.Run(args);

        // Assert
        result.Should().Be(0);
        _mockEmbeddedRunner.Verify(r => r.Run(args), Times.Once);
        _mockCliRunner.Verify(r => r.Run(It.IsAny<string[]>()), Times.Never);
        _mockInteractiveRunner.Verify(r => r.Run(), Times.Never);
        _mockLogger.Verify(l => l.LogModeDetected("Embedded"), Times.Once);
    }

    [Fact]
    public void ApplicationRunner_ShouldCallCliRunner_WhenModeIsCli()
    {
        // Arrange
        var args = new[] { "analyze", "log.txt" };
        var runner = new ApplicationRunner(_detector, _mockCliRunner.Object, _mockInteractiveRunner.Object, _mockEmbeddedRunner.Object, _mockLogger.Object);
        _mockCliRunner.Setup(r => r.Run(args)).Returns(0);

        // Act
        var result = runner.Run(args);

        // Assert
        result.Should().Be(0);
        _mockCliRunner.Verify(r => r.Run(args), Times.Once);
        _mockEmbeddedRunner.Verify(r => r.Run(It.IsAny<string[]>()), Times.Never);
        _mockInteractiveRunner.Verify(r => r.Run(), Times.Never);
        _mockLogger.Verify(l => l.LogModeDetected("Cli"), Times.Once);
    }

    [Fact]
    public void ApplicationRunner_ShouldCallInteractiveRunner_WhenModeIsInteractive()
    {
        // Arrange
        var args = Array.Empty<string>();
        var runner = new ApplicationRunner(_detector, _mockCliRunner.Object, _mockInteractiveRunner.Object, _mockEmbeddedRunner.Object, _mockLogger.Object);
        _mockInteractiveRunner.Setup(r => r.Run()).Returns(0);

        // Act
        var result = runner.Run(args);

        // Assert
        result.Should().Be(0);
        _mockInteractiveRunner.Verify(r => r.Run(), Times.Once);
        _mockCliRunner.Verify(r => r.Run(It.IsAny<string[]>()), Times.Never);
        _mockEmbeddedRunner.Verify(r => r.Run(It.IsAny<string[]>()), Times.Never);
        _mockLogger.Verify(l => l.LogModeDetected("Interactive"), Times.Once);
    }
}
