using FluentAssertions;
using SharkyParser.Core;

namespace SharkyParser.Tests;

public class LogAnalyzerTests
{
    private readonly LogAnalyzer _sut = new();

    [Fact]
    public void HasErrors_WithErrorEntries_ReturnsTrue()
    {
        var entries = new List<LogEntry>
        {
            new() { Level = "INFO", Message = "Starting" },
            new() { Level = "ERROR", Message = "Failed" }
        };

        _sut.HasErrors(entries).Should().BeTrue();
    }

    [Fact]
    public void HasErrors_WithFatalEntries_ReturnsTrue()
    {
        var entries = new List<LogEntry>
        {
            new() { Level = "FATAL", Message = "Crash" }
        };

        _sut.HasErrors(entries).Should().BeTrue();
    }

    [Fact]
    public void HasErrors_WithOnlyInfoEntries_ReturnsFalse()
    {
        var entries = new List<LogEntry>
        {
            new() { Level = "INFO", Message = "All good" }
        };

        _sut.HasErrors(entries).Should().BeFalse();
    }

    [Fact]
    public void HasWarnings_WithWarnEntries_ReturnsTrue()
    {
        var entries = new List<LogEntry>
        {
            new() { Level = "WARN", Message = "Be careful" }
        };

        _sut.HasWarnings(entries).Should().BeTrue();
    }

    [Fact]
    public void GetStatistics_MixedEntries_ReturnsCorrectCounts()
    {
        var entries = new List<LogEntry>
        {
            new() { Level = "INFO", Message = "Info 1" },
            new() { Level = "INFO", Message = "Info 2" },
            new() { Level = "WARN", Message = "Warning" },
            new() { Level = "ERROR", Message = "Error" }
        };

        var stats = _sut.GetStatistics(entries);

        stats.TotalCount.Should().Be(4);
        stats.InfoCount.Should().Be(2);
        stats.WarningCount.Should().Be(1);
        stats.ErrorCount.Should().Be(1);
        stats.IsHealthy.Should().BeFalse();
    }

    [Fact]
    public void GetStatistics_NoErrors_IsHealthyTrue()
    {
        var entries = new List<LogEntry>
        {
            new() { Level = "INFO", Message = "Good" },
            new() { Level = "WARN", Message = "Warning" }
        };

        var stats = _sut.GetStatistics(entries);

        stats.IsHealthy.Should().BeTrue();
    }

    [Fact]
    public void GetStatistics_EmptyList_ReturnsZeroCounts()
    {
        var entries = new List<LogEntry>();

        var stats = _sut.GetStatistics(entries);

        stats.TotalCount.Should().Be(0);
        stats.ErrorCount.Should().Be(0);
        stats.IsHealthy.Should().BeTrue();
    }
}
