using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using SharkyParser.Cli.Commands;
using SharkyParser.Cli.Infrastructure;
using SharkyParser.Core;
using SharkyParser.Core.Enums;
using SharkyParser.Core.Interfaces;
using SharkyParser.Core.Models;
using Spectre.Console.Cli;

namespace SharkyParser.Tests.Commands;

[Collection("Console")]
public class ParseCommandTests
{
    private static readonly object ConsoleLock = new();

    [Fact]
    public void Execute_EmbeddedOutputsStatsAndEscapedEntries()
    {
        var entries = new List<LogEntry>
        {
            new()
            {
                Timestamp = new DateTime(2024, 1, 2, 3, 4, 5, DateTimeKind.Utc),
                Level = "ERROR",
                Message = "bad|msg\nline2",
                Source = "Updater",
                LineNumber = 1,
                FilePath = "file.log",
                RawData = "raw"
            },
            new()
            {
                Timestamp = new DateTime(2024, 1, 2, 3, 4, 6, DateTimeKind.Utc),
                Level = "INFO",
                Message = "ok",
                Source = "Updater",
                LineNumber = 2,
                FilePath = "file.log",
                RawData = "raw2"
            }
        };
        var parser = new FakeLogParser(LogType.Update, "Test Parser", entries);
        var factory = new FakeLogParserFactory(parser);

        var logPath = Path.Combine(Path.GetTempPath(), $"parse_{Guid.NewGuid():N}.log");
        File.WriteAllText(logPath, "content");

        try
        {
            var output = RunCommand(factory, ["parse", logPath, "--type", "update", "--embedded", "--filter", "error"], out var exitCode);

            exitCode.Should().Be(0);
            factory.LastLogType.Should().Be(LogType.Update);
            factory.LastStackTraceMode.Should().Be(StackTraceMode.AllToStackTrace);

            output.Should().Contain("STATS|2|1|0|0|0");
            output.Should().Contain("bad\\|msg\\nline2");
            output.Should().Contain("stack\\|trace");
            output.Should().Contain("ENTRY|");
        }
        finally
        {
            File.Delete(logPath);
        }
    }

    [Fact]
    public void Execute_WithInvalidLogType_ReturnsError()
    {
        var parser = new FakeLogParser(LogType.Update, "Test Parser", []);
        var factory = new FakeLogParserFactory(parser);

        RunCommand(factory, ["parse", "file.log", "--type", "not-a-type"], out var exitCode);

        exitCode.Should().Be(1);
    }

    [Fact]
    public void Execute_WithMissingLogType_ReturnsError()
    {
        var parser = new FakeLogParser(LogType.Update, "Test Parser", []);
        var factory = new FakeLogParserFactory(parser);

        RunCommand(factory, ["parse", "file.log"], out var exitCode);

        exitCode.Should().Be(1);
    }

    [Fact]
    public void Execute_WithMissingFileInEmbeddedMode_ReturnsError()
    {
        var parser = new FakeLogParser(LogType.Update, "Test Parser", []);
        var factory = new FakeLogParserFactory(parser);

        RunCommand(factory, ["parse", "missing.log", "--type", "update", "--embedded"], out var exitCode);

        exitCode.Should().Be(1);
    }

    [Fact]
    public void Execute_WithMissingFile_NonEmbedded_ReturnsError()
    {
        var parser = new FakeLogParser(LogType.Update, "Test Parser", []);
        var factory = new FakeLogParserFactory(parser);

        RunCommand(factory, ["parse", "missing.log", "--type", "update"], out var exitCode);

        exitCode.Should().Be(1);
    }

    [Fact]
    public void Execute_NonEmbedded_ReturnsSuccess()
    {
        var entries = new List<LogEntry>
        {
            new() { Timestamp = DateTime.Now, Level = "ERROR", Message = "err", Source = "src" },
            new() { Timestamp = DateTime.Now, Level = "WARN", Message = "warn", Source = "src" },
            new() { Timestamp = DateTime.Now, Level = "INFO", Message = "info", Source = "src" },
            new() { Timestamp = DateTime.Now, Level = "DEBUG", Message = "dbg", Source = "src" }
        };
        var parser = new FakeLogParser(LogType.Update, "Test Parser", entries);
        var factory = new FakeLogParserFactory(parser);

        var logPath = Path.Combine(Path.GetTempPath(), $"parse_table_{Guid.NewGuid():N}.log");
        File.WriteAllText(logPath, "content");

        try
        {
            RunCommand(factory, ["parse", logPath, "--type", "update"], out var exitCode);

            exitCode.Should().Be(0);
        }
        finally
        {
            File.Delete(logPath);
        }
    }

    [Fact]
    public void Execute_WhenFactoryThrows_ReturnsError()
    {
        var factory = new ThrowingLogParserFactory();
        var logPath = Path.Combine(Path.GetTempPath(), $"parse_throw_{Guid.NewGuid():N}.log");
        File.WriteAllText(logPath, "content");

        try
        {
            var output = RunCommand(factory, ["parse", logPath, "--type", "update", "--embedded"], out var exitCode);

            exitCode.Should().Be(1);
            output.Should().Contain("ERROR|Parser error");
        }
        finally
        {
            File.Delete(logPath);
        }
    }

    [Fact]
    public void Execute_WhenFactoryThrows_NonEmbedded_ReturnsError()
    {
        var factory = new ThrowingLogParserFactory();
        var logPath = Path.Combine(Path.GetTempPath(), $"parse_throw_{Guid.NewGuid():N}.log");
        File.WriteAllText(logPath, "content");

        try
        {
            RunCommand(factory, ["parse", logPath, "--type", "update"], out var exitCode);

            exitCode.Should().Be(1);
        }
        finally
        {
            File.Delete(logPath);
        }
    }

    private static string RunCommand(ILogParserFactory factory, string[] args, out int exitCode)
    {
        var services = new ServiceCollection();
        services.AddSingleton(factory);

        var registrar = new TypeRegistrar(services);
        var app = new CommandApp(registrar);
        app.Configure(config => config.AddCommand<ParseCommand>("parse"));

        var writer = new StringWriter();
        var originalOut = Console.Out;
        lock (ConsoleLock)
        {
            Console.SetOut(writer);
            try
            {
                exitCode = app.Run(args);
            }
            finally
            {
                Console.SetOut(originalOut);
            }
        }

        return writer.ToString();
    }

    private sealed class FakeLogParserFactory : ILogParserFactory
    {
        private readonly ILogParser _parser;

        public FakeLogParserFactory(ILogParser parser)
        {
            _parser = parser;
        }

        public LogType? LastLogType { get; private set; }
        public StackTraceMode? LastStackTraceMode { get; private set; }

        public ILogParser CreateParser(LogType logType)
        {
            LastLogType = logType;
            return _parser;
        }

        public ILogParser CreateParser(LogType logType, StackTraceMode stackTraceMode)
        {
            LastLogType = logType;
            LastStackTraceMode = stackTraceMode;
            return _parser;
        }

        public IEnumerable<LogType> GetAvailableTypes() => new[] { _parser.SupportedLogType };

        public ILogParser GetParserForType(LogType logType) => _parser;
    }

    private sealed class FakeLogParser : ILogParser
    {
        private readonly IReadOnlyList<LogEntry> _entries;

        public FakeLogParser(LogType logType, string name, IReadOnlyList<LogEntry> entries)
        {
            SupportedLogType = logType;
            ParserName = name;
            _entries = entries;
        }

        public LogType SupportedLogType { get; }
        public string ParserName { get; }
        public string ParserDescription => "Test parser";
        public LogEntry? ParseLine(string line) => null;
        public IEnumerable<LogEntry> ParseFile(string path) => _entries;
        public Task<IEnumerable<LogEntry>> ParseFileAsync(string path)
            => Task.FromResult<IEnumerable<LogEntry>>(_entries);

        public IReadOnlyList<LogColumn> GetColumns() {
            throw new NotImplementedException();
        }
    }

    private sealed class ThrowingLogParserFactory : ILogParserFactory
    {
        public ILogParser CreateParser(LogType logType) => throw new InvalidOperationException("boom");
        public ILogParser CreateParser(LogType logType, StackTraceMode stackTraceMode) => throw new InvalidOperationException("boom");
        public IEnumerable<LogType> GetAvailableTypes() => Array.Empty<LogType>();
        public ILogParser GetParserForType(LogType logType) => throw new InvalidOperationException("boom");
    }
}
