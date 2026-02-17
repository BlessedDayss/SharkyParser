using SharkyParser.Cli;
using SharkyParser.Cli.Interfaces;
using SharkyParser.Cli.PreCheck;
using Moq;
using Xunit;

namespace SharkyParser.Tests.PreCheck;

public class ApplicationRunnerTests
{
    [Fact]
    public void Run_WithValidMocks_ExecutesWithoutThrowing()
    {
        var detector = new ApplicationModeDetector();
        var mockCliRunner = new Mock<ICliModeRunner>();
        var mockInteractiveRunner = new Mock<IInteractiveModeRunner>();
        var mockEmbeddedRunner = new Mock<IEmbeddedModeRunner>();
        var mockLogger = new Mock<IAppLogger>();
        var args = new[] { "parse", "test.log" };

        mockCliRunner.Setup(r => r.Run(It.IsAny<string[]>())).Returns(0);
        mockInteractiveRunner.Setup(r => r.Run()).Returns(0);
        mockEmbeddedRunner.Setup(r => r.Run(It.IsAny<string[]>())).Returns(0);

        var runner = new ApplicationRunner(
            detector,
            mockCliRunner.Object,
            mockInteractiveRunner.Object,
            mockEmbeddedRunner.Object,
            mockLogger.Object
        );

        var result = runner.Run(args);

        Assert.InRange(result, 0, 1);
    }

    [Fact]
    public void Run_WithNoArgs_ExecutesInteractiveMode()
    {
        var detector = new ApplicationModeDetector();
        var mockCliRunner = new Mock<ICliModeRunner>();
        var mockInteractiveRunner = new Mock<IInteractiveModeRunner>();
        var mockEmbeddedRunner = new Mock<IEmbeddedModeRunner>();
        var mockLogger = new Mock<IAppLogger>();
        var args = Array.Empty<string>();

        mockInteractiveRunner.Setup(r => r.Run()).Returns(0);

        var runner = new ApplicationRunner(
            detector,
            mockCliRunner.Object,
            mockInteractiveRunner.Object,
            mockEmbeddedRunner.Object,
            mockLogger.Object
        );

        var result = runner.Run(args);

        Assert.Equal(0, result);
        mockInteractiveRunner.Verify(r => r.Run(), Times.Once);
    }

    [Fact]
    public void Run_WithEmbeddedFlag_ExecutesEmbeddedMode()
    {
        var detector = new ApplicationModeDetector();
        var mockCliRunner = new Mock<ICliModeRunner>();
        var mockInteractiveRunner = new Mock<IInteractiveModeRunner>();
        var mockEmbeddedRunner = new Mock<IEmbeddedModeRunner>();
        var mockLogger = new Mock<IAppLogger>();
        var args = new[] { "--embedded" };

        mockEmbeddedRunner.Setup(r => r.Run(It.IsAny<string[]>())).Returns(0);

        var runner = new ApplicationRunner(
            detector,
            mockCliRunner.Object,
            mockInteractiveRunner.Object,
            mockEmbeddedRunner.Object,
            mockLogger.Object
        );

        var result = runner.Run(args);

        Assert.Equal(0, result);
        mockEmbeddedRunner.Verify(r => r.Run(args), Times.Once);
    }
}
