using FluentAssertions;
using SharkyParser.Core;

namespace SharkyParser.Tests.Core;

public class SourceExtractorTests
{
    [Fact]
    public void Extract_WithLevelAndSource_ReturnsSourceAndUpdatesMessage()
    {
        var message = "INFO   [Updater]   Completed";

        var source = SourceExtractor.Extract(ref message);

        source.Should().Be("Updater");
        message.Should().Be("Completed");
    }

    [Fact]
    public void Extract_WithLevelWithoutSource_ReturnsEmptyAndUpdatesMessage()
    {
        var message = "WARN   Something happened";

        var source = SourceExtractor.Extract(ref message);

        source.Should().BeEmpty();
        message.Should().Be("Something happened");
    }

    [Fact]
    public void Extract_WithMissingClosingBracket_ReturnsEmptyAndKeepsMessage()
    {
        var message = "[Updater Something happened";

        var source = SourceExtractor.Extract(ref message);

        source.Should().BeEmpty();
        message.Should().Be("[Updater Something happened");
    }

    [Fact]
    public void Extract_WhenLevelHasNoWhitespace_DoesNotStripPrefix()
    {
        var message = "INFOX [Updater] Something happened";

        var source = SourceExtractor.Extract(ref message);

        source.Should().BeEmpty();
        message.Should().Be("INFOX [Updater] Something happened");
    }
}
