using FluentAssertions;
using Moq;
using SharkyParser.Api.Data.Models;
using SharkyParser.Api.Data.Repositories;
using SharkyParser.Api.Services;
using SharkyParser.Core;
using SharkyParser.Core.Enums;
using SharkyParser.Core.Interfaces;
using SharkyParser.Core.Models;

namespace SharkyParser.Tests.Api;

public class LogParsingServiceTests
{
    [Fact]
    public async Task ParseFileAsync_ConfiguresTeamCityBlocks_WhenParserSupportsIt()
    {
        var parser = new TeamCityConfigurableParserFake();
        var parserFactory = new Mock<ILogParserFactory>();
        parserFactory.Setup(f => f.CreateParser(LogType.TeamCity)).Returns(parser);

        var analyzer = new Mock<ILogAnalyzer>();
        analyzer.Setup(a => a.GetStatistics(It.IsAny<IEnumerable<LogEntry>>(), LogType.TeamCity))
            .Returns(new LogStatistics(1, 0, 0, 1, 0, true));

        var repository = new Mock<IFileRepository>();
        repository.Setup(r => r.AddAsync(It.IsAny<FileRecord>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var service = new LogParsingService(
            repository.Object,
            parserFactory.Object,
            analyzer.Object,
            Mock.Of<SharkyParser.Core.Interfaces.ILogger>());

        await using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes("test"));

        await service.ParseFileAsync(
            stream,
            "sample.log",
            LogType.TeamCity,
            new[] { "Test-BeforeUpdate", "Update-App" },
            CancellationToken.None);

        parser.ConfiguredBlocks.Should().Equal("Test-BeforeUpdate", "Update-App");
    }

    [Fact]
    public async Task GetEntriesAsync_ConfiguresTeamCityBlocks_WhenParserSupportsIt()
    {
        var parser = new TeamCityConfigurableParserFake();
        var parserFactory = new Mock<ILogParserFactory>();
        parserFactory.Setup(f => f.CreateParser(LogType.TeamCity)).Returns(parser);

        var analyzer = new Mock<ILogAnalyzer>();
        analyzer.Setup(a => a.GetStatistics(It.IsAny<IEnumerable<LogEntry>>(), LogType.TeamCity))
            .Returns(new LogStatistics(1, 0, 0, 1, 0, true));

        var repository = new Mock<IFileRepository>();
        repository.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FileRecord
            {
                Id = Guid.NewGuid(),
                FileName = "sample.log",
                FileSize = 4,
                LogType = LogType.TeamCity.ToString(),
                Content = System.Text.Encoding.UTF8.GetBytes("test")
            });

        var service = new LogParsingService(
            repository.Object,
            parserFactory.Object,
            analyzer.Object,
            Mock.Of<SharkyParser.Core.Interfaces.ILogger>());

        await service.GetEntriesAsync(
            Guid.NewGuid(),
            new[] { "Update-App" },
            CancellationToken.None);

        parser.ConfiguredBlocks.Should().Equal("Update-App");
    }

    private sealed class TeamCityConfigurableParserFake : ILogParser, ITeamCityBlockConfigurableParser
    {
        public IReadOnlyList<string> ConfiguredBlocks { get; private set; } = Array.Empty<string>();

        public LogType SupportedLogType => LogType.TeamCity;
        public string ParserName => "TeamCity";
        public string ParserDescription => "TeamCity parser fake";

        public void ConfigureBlocks(IEnumerable<string>? blocks)
        {
            ConfiguredBlocks = blocks?.ToArray() ?? Array.Empty<string>();
        }

        public LogEntry? ParseLine(string line) => null;

        public IEnumerable<LogEntry> ParseFile(string path)
        {
            return
            [
                new LogEntry
                {
                    Timestamp = DateTime.UtcNow,
                    Level = "INFO",
                    Message = "entry",
                    FilePath = path,
                    LineNumber = 1,
                    RawData = "entry"
                }
            ];
        }

        public Task<IEnumerable<LogEntry>> ParseFileAsync(string path)
            => Task.FromResult(ParseFile(path));

        public IReadOnlyList<LogColumn> GetColumns()
            => new List<LogColumn>
            {
                new("Timestamp", "Timestamp", "ts", true),
                new("Level", "Level", "level", true),
                new("Message", "Message", "message", true)
            };
    }
}
