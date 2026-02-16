using FluentAssertions;
using Moq;
using SharkyParser.Core;
using SharkyParser.Core.Enums;
using SharkyParser.Core.Interfaces;
using SharkyParser.Core.Parsers;
using Xunit;

namespace SharkyParser.Tests.Parsers;

public class BaseLogParserTests
{
    [Fact]
    public void ParseLine_WhenParseLineCoreThrows_ReturnsErrorEntryAndLogs()
    {
        var logger = new Mock<IAppLogger>();
        var parser = new ThrowingParser(logger.Object);

        var entry = parser.ParseLine("bad line");

        entry.Should().NotBeNull();
        entry!.Level.Should().Be("ERROR");
        entry.Message.Should().StartWith("Parse error:");
        entry.RawData.Should().Be("bad line");
        entry.Source.Should().Be(parser.ParserName);

        logger.Verify(
            l => l.LogError(
                It.Is<string>(msg => msg.Contains("Failed to parse line: bad line")),
                It.Is<Exception?>(ex => ex == null)),
            Times.Once);
    }

    private static readonly string[] TestContent = ["first", "", "second"];
    private static readonly string[] AsyncTestContent = ["first", "second"];

    [Fact]
    public void ParseFile_AssignsFilePathAndLineNumbers()
    {
        var logger = new Mock<IAppLogger>();
        var parser = new PassThroughParser(logger.Object);

        var path = Path.Combine(Path.GetTempPath(), $"parser_{Guid.NewGuid():N}.log");
        File.WriteAllLines(path, TestContent);

        try
        {
            var entries = parser.ParseFile(path).ToList();

            entries.Should().HaveCount(2);
            entries[0].FilePath.Should().Be(path);
            entries[0].LineNumber.Should().Be(1);
            entries[0].Message.Should().Be("first");
            entries[1].LineNumber.Should().Be(3);
            entries[1].Message.Should().Be("second");
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public async Task ParseFileAsync_ReadsEntries()
    {
        var logger = new Mock<IAppLogger>();
        var parser = new PassThroughParser(logger.Object);

        var path = Path.Combine(Path.GetTempPath(), $"parser_async_{Guid.NewGuid():N}.log");
        File.WriteAllLines(path, AsyncTestContent);

        try
        {
            var entries = (await parser.ParseFileAsync(path)).ToList();

            entries.Should().HaveCount(2);
            entries[0].Message.Should().Be("first");
            entries[1].Message.Should().Be("second");
        }
        finally
        {
            File.Delete(path);
        }
    }

    private sealed class ThrowingParser : BaseLogParser
    {
        public ThrowingParser(IAppLogger logger) : base(logger) { }
        public override LogType SupportedLogType => LogType.Installation;
        public override string ParserName => "Throwing Parser";
        public override string ParserDescription => "Throwing Parser";
        protected override LogEntry? ParseLineCore(string line) => throw new InvalidOperationException("boom");
    }

    private sealed class PassThroughParser : BaseLogParser
    {
        public PassThroughParser(IAppLogger logger) : base(logger) { }
        public override LogType SupportedLogType => LogType.Update;
        public override string ParserName => "PassThrough";
        public override string ParserDescription => "PassThrough";
        protected override LogEntry? ParseLineCore(string line) => new() { Message = line, RawData = line };
    }
}
