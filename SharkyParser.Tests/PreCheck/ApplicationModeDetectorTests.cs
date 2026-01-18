using SharkyParser.Cli.PreCheck;
using Moq;
using Xunit;

namespace SharkyParser.Tests.PreCheck;

public class ApplicationModeDetectorTests
{
    [Fact]
    public void DetermineMode_WithEmbeddedFlag_ReturnsEmbeddedMode()
    {
        // Arrange
        var detector = new ApplicationModeDetector();
        var args = new[] { "--embedded" };

        // Act
        var result = detector.DetermineMode(args);

        // Assert
        Assert.Equal(ApplicationMode.Embedded, result);
    }

    [Fact]
    public void DetermineMode_WithEmbeddedFlagCaseInsensitive_ReturnsEmbeddedMode()
    {
        // Arrange
        var detector = new ApplicationModeDetector();
        var args = new[] { "--EMBEDDED" };

        // Act
        var result = detector.DetermineMode(args);

        // Assert
        Assert.Equal(ApplicationMode.Embedded, result);
    }

    [Fact]
    public void DetermineMode_WithCliArgs_ReturnsCliMode()
    {
        // Arrange
        var detector = new ApplicationModeDetector();
        var args = new[] { "parse", "file.log" };

        // Act
        var result = detector.DetermineMode(args);

        // Assert
        Assert.Equal(ApplicationMode.Cli, result);
    }

    [Fact]
    public void DetermineMode_WithNoArgs_ReturnsInteractiveMode()
    {
        // Arrange
        var detector = new ApplicationModeDetector();
        var args = Array.Empty<string>();

        // Act
        var result = detector.DetermineMode(args);

        // Assert
        Assert.Equal(ApplicationMode.Interactive, result);
    }

    [Fact]
    public void DetermineMode_WithEmbeddedAndOtherArgs_ReturnsEmbeddedMode()
    {
        // Arrange
        var detector = new ApplicationModeDetector();
        var args = new[] { "parse", "file.log", "--embedded" };

        // Act
        var result = detector.DetermineMode(args);

        // Assert
        Assert.Equal(ApplicationMode.Embedded, result);
    }
}
