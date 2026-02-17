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
    public void ParseLine_WithTimeOnlyFormat_ParsesEntryUsingBaseDate()
    {
        var logger = new Mock<ILogger>();
        var parser = new UpdateLogParser(logger.Object);
        var baseDate = new DateTime(2023, 10, 27);

        var tempFile = Path.GetTempFileName();
        var fileNameWithDate = "Update_2023_10_27_log.txt";
        var fullPath = Path.Combine(Path.GetDirectoryName(tempFile)!, fileNameWithDate);
        File.WriteAllText(fullPath, "16:34:18.3717 Current Updater version: '2.4.0.0'");

        try
        {
            var entries = parser.ParseFile(fullPath).ToList();

            entries.Should().HaveCount(1);
            entries[0].Timestamp.Should().BeCloseTo(baseDate.Add(new TimeSpan(0, 16, 34, 18, 371)), TimeSpan.FromMilliseconds(5));
            entries[0].Message.Should().Be("Current Updater version: '2.4.0.0'");
            entries[0].Level.Should().Be("INFO");
        }
        finally
        {
            File.Delete(fullPath);
            File.Delete(tempFile);
        }
    }

    [Fact]
    public void ParseLine_WithTimeOnlyFormat_DetectedErrorLevel()
    {
        var logger = new Mock<ILogger>();
        var parser = new UpdateLogParser(logger.Object);

        var tempFile = Path.GetTempFileName();
        File.WriteAllText(tempFile, "16:34:18.3717 Error: Connection failed");

        try
        {
            var entries = parser.ParseFile(tempFile).ToList();

            entries.Should().HaveCount(1);
            entries[0].Level.Should().Be("ERROR");
        }
        finally
        {
            File.Delete(tempFile);
        }
    }
}
