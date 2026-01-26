using FluentAssertions;
using Moq;
using SharkyParser.Core.Interfaces;
using SharkyParser.Core.Parsers;

namespace SharkyParser.Tests.Parsers;

public class IisLogParserTests
{
    [Fact]
    public void ParseLine_WhenNotImplemented_ReturnsErrorEntryAndLogs()
    {
        var logger = new Mock<IAppLogger>();
        var parser = new IISLogParser(logger.Object);

        var entry = parser.ParseLine("any line");

        entry.Should().NotBeNull();
        entry!.Level.Should().Be("ERROR");
        entry.Source.Should().Be(parser.ParserName);

        logger.Verify(
            l => l.LogError(
                It.Is<string>(msg => msg.Contains("Failed to parse line: any line")),
                It.IsAny<Exception>()),
            Times.Once);
    }
}
