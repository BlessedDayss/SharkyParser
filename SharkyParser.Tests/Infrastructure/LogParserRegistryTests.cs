using FluentAssertions;
using Moq;
using SharkyParser.Core;
using SharkyParser.Core.Enums;
using SharkyParser.Core.Infrastructure;
using SharkyParser.Core.Interfaces;

namespace SharkyParser.Tests.Infrastructure;

public class LogParserRegistryTests
{
    [Fact]
    public void Ctor_RegistersDefaultParsers()
    {
        var logger = new Mock<IAppLogger>();
        var registry = new LogParserRegistry(logger.Object);

        registry.IsRegistered(LogType.Installation).Should().BeTrue();
        registry.IsRegistered(LogType.Update).Should().BeTrue();
        registry.IsRegistered(LogType.IIS).Should().BeTrue();
        registry.IsRegistered(LogType.RabbitMq).Should().BeFalse();
    }

    [Fact]
    public void Register_WhenTypeDoesNotImplementILogParser_Throws()
    {
        var logger = new Mock<IAppLogger>();
        var registry = new LogParserRegistry(logger.Object);

        Action action = () => registry.Register(LogType.RabbitMq, typeof(NotAParser));

        action.Should().Throw<ArgumentException>()
            .WithMessage("*does not implement ILogParser*");
    }

    [Fact]
    public void Register_AddsParserType()
    {
        var logger = new Mock<IAppLogger>();
        var registry = new LogParserRegistry(logger.Object);

        registry.Register(LogType.RabbitMq, typeof(TestParser));

        registry.IsRegistered(LogType.RabbitMq).Should().BeTrue();
        registry.GetParserType(LogType.RabbitMq).Should().Be(typeof(TestParser));
    }

    [Fact]
    public void GetParserType_WhenNotRegistered_Throws()
    {
        var logger = new Mock<IAppLogger>();
        var registry = new LogParserRegistry(logger.Object);

        Action action = () => registry.GetParserType(LogType.RabbitMq);

        action.Should().Throw<ArgumentException>()
            .WithMessage("*No parser registered*");
    }

    private sealed class NotAParser
    {
    }

    private sealed class TestParser : ILogParser
    {
        public LogType SupportedLogType => LogType.RabbitMq;
        public string ParserName => "Test";
        public string ParserDescription => "Test parser";
        public LogEntry? ParseLine(string line) => null;
        public IEnumerable<LogEntry> ParseFile(string path) => Array.Empty<LogEntry>();

        public Task<IEnumerable<LogEntry>> ParseFileAsync(string path)
            => Task.FromResult<IEnumerable<LogEntry>>(Array.Empty<LogEntry>());
    }
}
