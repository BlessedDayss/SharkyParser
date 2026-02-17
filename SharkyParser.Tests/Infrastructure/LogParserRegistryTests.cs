using FluentAssertions;
using SharkyParser.Core.Enums;
using SharkyParser.Core.Infrastructure;
using SharkyParser.Core.Interfaces;
using SharkyParser.Core.Models;
using Xunit;

namespace SharkyParser.Tests.Infrastructure;

public class LogParserRegistryTests
{
    [Fact]
    public void NewRegistry_HasNoParsersRegistered()
    {
        var registry = new LogParserRegistry();

        registry.IsRegistered(LogType.Installation).Should().BeFalse();
        registry.IsRegistered(LogType.Update).Should().BeFalse();
    }

    [Fact]
    public void Register_AddsParser_IsRegisteredReturnsTrue()
    {
        var registry = new LogParserRegistry();
        registry.Register(LogType.Installation, () => new TestParser(LogType.Installation));

        registry.IsRegistered(LogType.Installation).Should().BeTrue();
    }

    [Fact]
    public void Register_OverwritesExistingEntry()
    {
        var registry = new LogParserRegistry();
        var first = new TestParser(LogType.Installation);
        var second = new TestParser(LogType.Installation);

        registry.Register(LogType.Installation, () => first);
        registry.Register(LogType.Installation, () => second);

        registry.CreateParser(LogType.Installation).Should().BeSameAs(second);
    }

    [Fact]
    public void Register_WithNullFactory_ThrowsArgumentNullException()
    {
        var registry = new LogParserRegistry();

        Action action = () => registry.Register(LogType.Update, null!);

        action.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void CreateParser_CallsFactoryDelegate()
    {
        var registry = new LogParserRegistry();
        var parser = new TestParser(LogType.Update);
        registry.Register(LogType.Update, () => parser);

        var result = registry.CreateParser(LogType.Update);

        result.Should().BeSameAs(parser);
    }

    [Fact]
    public void CreateParser_WhenNotRegistered_ThrowsArgumentException()
    {
        var registry = new LogParserRegistry();

        Action action = () => registry.CreateParser(LogType.RabbitMq);

        action.Should().Throw<ArgumentException>()
            .WithMessage("*No parser registered*");
    }

    [Fact]
    public void GetRegisteredTypes_ReturnsAllRegisteredTypes()
    {
        var registry = new LogParserRegistry();
        registry.Register(LogType.Installation, () => new TestParser(LogType.Installation));
        registry.Register(LogType.IIS,          () => new TestParser(LogType.IIS));

        registry.GetRegisteredTypes().Should().BeEquivalentTo(
            new[] { LogType.Installation, LogType.IIS });
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private sealed class TestParser : ILogParser
    {
        private readonly LogType _type;
        public TestParser(LogType type) => _type = type;

        public LogType SupportedLogType => _type;
        public string ParserName => "Test";
        public string ParserDescription => "Test parser";

        public LogEntry? ParseLine(string line) => null;
        public IEnumerable<LogEntry> ParseFile(string path) => Array.Empty<LogEntry>();
        public Task<IEnumerable<LogEntry>> ParseFileAsync(string path)
            => Task.FromResult<IEnumerable<LogEntry>>(Array.Empty<LogEntry>());
        public IReadOnlyList<LogColumn> GetColumns() => Array.Empty<LogColumn>();
    }
}
