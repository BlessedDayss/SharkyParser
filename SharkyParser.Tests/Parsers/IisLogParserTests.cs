using FluentAssertions;
using Moq;
using SharkyParser.Core.Interfaces;
using SharkyParser.Core.Parsers;
using SharkyParser.Core.Models;
using Xunit;

namespace SharkyParser.Tests.Parsers;

public class IISLogParserTests
{
    private readonly Mock<IAppLogger> _logger;
    private readonly IISLogParser _parser;

    public IISLogParserTests()
    {
        _logger = new Mock<IAppLogger>();
        _parser = new IISLogParser(_logger.Object);
    }

    [Fact]
    public void ParseLine_WithFieldsDirective_SetsHeadersAndReturnsNull()
    {
        var line = "#Fields: date time s-ip cs-method";
        var entry = _parser.ParseLine(line);

        entry.Should().BeNull();
    }

    [Fact]
    public void ParseLine_WithValidData_ReturnsLogEntryWithFields()
    {
        _parser.ParseLine("#Fields: date time c-ip cs-method cs-uri-stem sc-status");
        var line = "2026-02-17 13:00:01 10.0.0.1 GET /index.html 200";
        
        var entry = _parser.ParseLine(line);

        entry.Should().NotBeNull();
        entry!.Timestamp.Should().Be(new DateTime(2026, 2, 17, 13, 0, 1, DateTimeKind.Utc));
        entry.Fields["c-ip"].Should().Be("10.0.0.1");
        entry.Fields["cs-method"].Should().Be("GET");
        entry.Fields["cs-uri-stem"].Should().Be("/index.html");
        entry.Fields["sc-status"].Should().Be("200");
        entry.Message.Should().Be("/index.html");
    }

    [Fact]
    public void ParseLine_WithMissingValues_HandlesDashes()
    {
        _parser.ParseLine("#Fields: date time cs-uri-query sc-status");
        var line = "2026-02-17 13:00:01 - 404";

        var entry = _parser.ParseLine(line);

        entry.Should().NotBeNull();
        entry!.Fields.Should().NotContainKey("cs-uri-query");
        entry.Fields["sc-status"].Should().Be("404");
    }

    [Fact]
    public void ParseLine_WithQuotedFields_SplitsCorrectly()
    {
        _parser.ParseLine("#Fields: date time cs(User-Agent) sc-status");
        var line = "2026-02-17 13:00:01 \"Mozilla/5.0 (Windows NT 10.0)\" 200";

        var entry = _parser.ParseLine(line);

        entry.Should().NotBeNull();
        entry!.Fields["cs(User-Agent)"].Should().Be("Mozilla/5.0 (Windows NT 10.0)");
        entry.Fields["sc-status"].Should().Be("200");
    }
}
