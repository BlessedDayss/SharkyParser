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
    public void ParseLine_InvalidFormat_ReturnsEntryWithMessage()
    {
        var result = _sut.ParseLine("This is not a valid log line");

        result.Should().NotBeNull();
        result!.Message.Should().Be("This is not a valid log line");
        result.Timestamp.Should().Be(DateTime.MinValue);
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
        result.Level.Should().Be("WARN");
    }

    [Fact]
    public void ParseLine_TimeOnlyFormat_ParsesCorrectly()
    {
        var result = _sut.ParseLine("14:30:45 ERROR Something failed");

        result.Should().NotBeNull();
        result!.Timestamp.TimeOfDay.Should().Be(new TimeSpan(14, 30, 45));
        result.Level.Should().Be("ERROR");
    }

    [Fact]
    public void ParseLine_ErrorKeywordInMessage_DetectsAsError()
    {
        var result = _sut.ParseLine("2025-01-01 12:00:00 Application failed to start");

        result.Should().NotBeNull();
        result!.Level.Should().Be("ERROR");
    }

    [Fact]
    public void ParseLine_ZeroErrors_NotFalsePositive()
    {
        var result = _sut.ParseLine("2025-01-01 12:00:00 Build completed: 0 errors");

        result.Should().NotBeNull();
        result!.Level.Should().Be("INFO");
    }

    [Fact]
    public void ParseLine_FatalLevel_DetectedCorrectly()
    {
        var result = _sut.ParseLine("2025-01-01 12:00:00 FATAL System crash");

        result.Should().NotBeNull();
        result!.Level.Should().Be("FATAL");
    }

    [Fact]
    public void ParseLine_CriticalLevel_DetectedCorrectly()
    {
        var result = _sut.ParseLine("2025-01-01 12:00:00 CRITICAL Out of memory");

        result.Should().NotBeNull();
        result!.Level.Should().Be("CRITICAL");
    }

    [Fact]
    public void ParseLine_DebugLevel_DetectedCorrectly()
    {
        var result = _sut.ParseLine("2025-01-01 12:00:00 DEBUG Entering method");

        result.Should().NotBeNull();
        result!.Level.Should().Be("DEBUG");
    }

    [Fact]
    public void ParseLine_DotMilliseconds_ParsesCorrectly()
    {
        var result = _sut.ParseLine("2025-01-01 12:45:33.421 INFO [App] Message");

        result.Should().NotBeNull();
        result!.Timestamp.Should().Be(new DateTime(2025, 1, 1, 12, 45, 33, 421));
    }
}

public class LogParserExtraTests
{
    private readonly LogParser _sut = new();

    [Fact]
    public void ParseLine_MessageIsWarning_DetectsAsWarn()
    {
        var result = _sut.ParseLine("11:30:40 WARNING");
        
        result.Should().NotBeNull();
        result!.Level.Should().Be("WARN");
    }
    
    [Fact]  
    public void ParseLine_MessageIsError_DetectsAsError()
    {
        var result = _sut.ParseLine("12:30:40 ERROR");
        
        result.Should().NotBeNull();
        result!.Level.Should().Be("ERROR");
    }
}
