using FluentAssertions;
using SharkyParser.Core;

namespace SharkyParser.Tests.Core;

public class TimestampParserTests
{
    [Fact]
    public void TryParse_LineWithDateTime_ReturnsTrueAndLength()
    {
        var line = "2024-01-02 03:04:05 message";

        var success = TimestampParser.TryParse(line, out var result, out var length);

        success.Should().BeTrue();
        result.Should().Be(new DateTime(2024, 1, 2, 3, 4, 5));
        length.Should().Be(19);
    }

    [Fact]
    public void TryParse_LineWithTimeOnly_ReturnsTrueAndLength()
    {
        var line = "01:02:03 rest";

        var success = TimestampParser.TryParse(line, out var result, out var length);

        success.Should().BeTrue();
        result.TimeOfDay.Should().Be(new TimeSpan(1, 2, 3));
        result.Date.Should().Be(DateTime.Today);
        length.Should().Be(8);
    }

    [Fact]
    public void TryParse_LineWithoutWhitespaceAfterTimestamp_ReturnsFalse()
    {
        var line = "2024-01-02 03:04:05X";

        var success = TimestampParser.TryParse(line, out _, out var length);

        success.Should().BeFalse();
        length.Should().Be(0);
    }

    [Fact]
    public void TryParse_LineStartingWithNonDigit_ReturnsFalse()
    {
        var line = "X2024-01-02 03:04:05";

        var success = TimestampParser.TryParse(line, out _, out var length);

        success.Should().BeFalse();
        length.Should().Be(0);
    }

    [Fact]
    public void TryParse_WithWhitespaceLine_ReturnsFalse()
    {
        var success = TimestampParser.TryParse("   ", out _, out var length);

        success.Should().BeFalse();
        length.Should().Be(0);
    }

    [Fact]
    public void TryParse_TextWithSupportedFormat_ReturnsTrue()
    {
        var success = TimestampParser.TryParse("2024/01/02 03:04:05", out var result);

        success.Should().BeTrue();
        result.Should().Be(new DateTime(2024, 1, 2, 3, 4, 5));
    }

    [Fact]
    public void TryParse_TextWithInvalidFormat_ReturnsFalse()
    {
        var success = TimestampParser.TryParse("not a date", out _);

        success.Should().BeFalse();
    }
}
