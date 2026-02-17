using FluentAssertions;
using Moq;
using SharkyParser.Core.Interfaces;
using SharkyParser.Core.Parsers;
using SharkyParser.Core.Models;
using Xunit;
using ILogger = SharkyParser.Core.Interfaces.ILogger;

namespace SharkyParser.Tests.Parsers;

public class UpdateLogParserTests
{
    [Fact]
    public void ParseLine_WithTimestamp_ParsesEntry()
    {
        var logger = new Mock<ILogger>();
        var parser = new UpdateLogParser(logger.Object);

        var entry = parser.ParseLine("2024-01-02 03:04:05 [Updater] Installing package: Success");

        entry.Should().NotBeNull();
        entry!.Timestamp.Should().Be(new DateTime(2024, 1, 2, 3, 4, 5));
        entry.Level.Should().Be("INFO");
        entry.Fields["Component"].Should().Be("Updater");
        entry.Message.Should().Be("Installing package: Success");
        entry.RawData.Should().Be("2024-01-02 03:04:05 [Updater] Installing package: Success");
    }

    [Fact]
    public void ParseLine_WithFailedStatus_ReturnsErrorLevel()
    {
        var logger = new Mock<ILogger>();
        var parser = new UpdateLogParser(logger.Object);

        var entry = parser.ParseLine("[Updater] Installing package: Failed");

        entry.Should().NotBeNull();
        entry!.Level.Should().Be("ERROR");
        entry.Timestamp.Should().BeCloseTo(DateTime.Now, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void ParseLine_WithWarningStatus_ReturnsWarnLevel()
    {
        var logger = new Mock<ILogger>();
        var parser = new UpdateLogParser(logger.Object);

        var entry = parser.ParseLine("[Updater] Installing package: Warning");

        entry.Should().NotBeNull();
        entry!.Level.Should().Be("WARN");
    }

    [Fact]
    public void ParseLine_WhenNoMatch_ReturnsNull()
    {
        var logger = new Mock<ILogger>();
        var parser = new UpdateLogParser(logger.Object);

        var entry = parser.ParseLine("Some other line");

        entry.Should().BeNull();
    }
}
