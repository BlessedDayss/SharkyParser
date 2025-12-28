using FluentAssertions;
using SharkyParser.Core;

namespace TestProject1SharkyParser.Tests;

public class LogParserTests
{
    private readonly LogParser _sut = new();

    [Fact]
    public void ParseLine_ValidLogLine_ReturnsLogEntry()
    {
        var result = _sut.ParseLine("2025-01-01 12:45:33,421 INFO [App] Hello World");

        result.Should().NotBeNull();
        result!.Level.Should().Be("INFO");
        result.Source.Should().Be("App");
        result.Message.Should().Be("Hello World");
        result.Timestamp.Should().Be(new DateTime(2025, 1, 1, 12, 45, 33, 421));
    }

    [Fact]
    public void ParseLine_ErrorLevel_ReturnsCorrectLevel()
    {
        var result = _sut.ParseLine("2025-01-01 12:45:33,421 ERROR [Database] Connection failed");

        result.Should().NotBeNull();
        result!.Level.Should().Be("ERROR");
    }

    [Fact]
    public void ParseLine_InvalidFormat_ReturnsNull()
    {
        var result = _sut.ParseLine("This is not a valid log line");

        result.Should().BeNull();
    }

    [Fact]
    public void ParseLine_EmptyString_ReturnsNull()
    {
        var result = _sut.ParseLine("");

        result.Should().BeNull();
    }

    [Fact]
    public void ParseLine_SourceWithBrackets_TrimsBrackets()
    {
        var result = _sut.ParseLine("2025-01-01 12:45:33,421 WARN [MyService] Warning message");

        result.Should().NotBeNull();
        result!.Source.Should().Be("MyService");
        result.Source.Should().NotContain("[");
        result.Source.Should().NotContain("]");
    }
}
