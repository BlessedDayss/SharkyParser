using FluentAssertions;
using SharkyParser.Core;

namespace SharkyParser.Tests.Core;

public class LevelDetectorTests
{
    [Theory]
    [InlineData("ERROR Something", LogLevel.Error)]
    [InlineData("ERR Something", LogLevel.Error)]
    [InlineData("ERRO Something", LogLevel.Error)]
    [InlineData("FATAL Crash", LogLevel.Error)]
    [InlineData("CRITICAL Crash", LogLevel.Error)]
    public void Detect_WithErrorPrefixes_ReturnsError(string line, string expected)
    {
        LevelDetector.Detect(line).Should().Be(expected);
    }

    [Theory]
    [InlineData("WARN Something", LogLevel.Warn)]
    [InlineData("WARNING Something", LogLevel.Warn)]
    public void Detect_WithWarnPrefixes_ReturnsWarn(string line, string expected)
    {
        LevelDetector.Detect(line).Should().Be(expected);
    }

    [Theory]
    [InlineData("DEBUG Details", LogLevel.Debug)]
    [InlineData("DBG Details", LogLevel.Debug)]
    public void Detect_WithDebugPrefixes_ReturnsDebug(string line, string expected)
    {
        LevelDetector.Detect(line).Should().Be(expected);
    }

    [Fact]
    public void Detect_WithTracePrefix_ReturnsTrace()
    {
        LevelDetector.Detect("TRACE Step").Should().Be(LogLevel.Trace);
    }

    [Fact]
    public void Detect_WithInfoPrefix_ReturnsInfo()
    {
        LevelDetector.Detect("INFO Start").Should().Be(LogLevel.Info);
    }

    [Fact]
    public void Detect_WithEmpty_ReturnsInfo()
    {
        LevelDetector.Detect(" ").Should().Be(LogLevel.Info);
    }

    [Theory]
    [InlineData("Unhandled exception occurred", LogLevel.Error)]
    [InlineData("Operation failed due to timeout", LogLevel.Error)]
    [InlineData("Caution: disk is almost full", LogLevel.Warn)]
    public void Detect_WithKeywords_ReturnsExpectedLevel(string line, string expected)
    {
        LevelDetector.Detect(line).Should().Be(expected);
    }

    [Theory]
    [InlineData("0 error(s) found")]
    [InlineData("no error found")]
    [InlineData("(18/126) Processing step 'ResetError.sql'")]
    [InlineData("Step 'Resetrror.sql' result: 'successful'")]
    public void Detect_WithFalsePositive_ReturnsInfo(string line)
    {
        LevelDetector.Detect(line).Should().Be(LogLevel.Info);
    }
}
