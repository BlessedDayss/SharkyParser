using FluentAssertions;
using Moq;
using SharkyParser.Core.Enums;
using SharkyParser.Core.Interfaces;
using SharkyParser.Core.Parsers;
using Xunit;

namespace SharkyParser.Tests.Parsers;

public class InstallationLogParserTests
{
    [Fact]
    public void ParseFile_AllToStackTrace_AppendsToStackTrace()
    {
        var logger = new Mock<IAppLogger>();
        var parser = new InstallationLogParser(logger.Object);
        var path = Path.Combine(Path.GetTempPath(), $"2024_12_31_install_log_extra_{Guid.NewGuid():N}.log");
        var lines = new[] { "[01:02:03] INFO First line", "second line", "third line" };
        File.WriteAllLines(path, lines);

        try
        {
            var entries = parser.ParseFile(path).ToList();

            entries.Should().HaveCount(1);
            var entry = entries[0];
            entry.Timestamp.Should().Be(new DateTime(2024, 12, 31, 1, 2, 3));
            entry.Level.Should().Be("INFO");
            entry.Message.Should().Be("INFO First line");
            entry.StackTrace.Should().Be($"second line{Environment.NewLine}third line");
            entry.RawData.Should().Be(string.Join(Environment.NewLine, lines));
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void ParseFile_NoStackTrace_AppendsToMessage()
    {
        var logger = new Mock<IAppLogger>();
        var parser = new InstallationLogParser(logger.Object)
        {
            StackTraceMode = StackTraceMode.NoStackTrace
        };
        var path = Path.Combine(Path.GetTempPath(), $"2024_12_31_install_log_extra_{Guid.NewGuid():N}.log");
        var lines = new[] { "[01:02:03] INFO First line", "second line", "third line" };
        File.WriteAllLines(path, lines);

        try
        {
            var entry = parser.ParseFile(path).Single();

            entry.StackTrace.Should().BeEmpty();
            entry.Message.Should().Be($"INFO First line{Environment.NewLine}second line{Environment.NewLine}third line");
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void ParseLine_WithFullTimestamp_ParsesTimestampAndLevel()
    {
        var logger = new Mock<IAppLogger>();
        var parser = new InstallationLogParser(logger.Object);

        var entry = parser.ParseLine("2024-01-02 03:04:05,123 ERROR Something bad");

        entry.Should().NotBeNull();
        entry!.Timestamp.Should().Be(new DateTime(2024, 1, 2, 3, 4, 5, 123));
        entry.Level.Should().Be("ERROR");
        entry.Message.Should().Be("ERROR Something bad");
    }
}
