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
public class AnalyzeCommandTests
{
    private static readonly object ConsoleLock = new();

    [Fact]
    public void Execute_EmbeddedOutputsAnalysisLine()
    {
        var entries = new List<LogEntry>
        {
            new() {
                Level = "ERROR",
                Message = "bad",
                Timestamp = default
            },
            new() {
                Level = "INFO",
                Message = "ok",
                Timestamp = default
            }
        };
        var parser = new FakeLogParser(LogType.Update, entries);
        var factory = new FakeLogParserFactory(parser);
        var analyzer = new FakeAnalyzer(new LogStatistics(2, 1, 0, 1, 0, false, "extra"));

        var logPath = Path.Combine(Path.GetTempPath(), $"analyze_{Guid.NewGuid():N}.log");
        File.WriteAllText(logPath, "content");

        try
        {
            var output = RunCommand(factory, analyzer, ["analyze", logPath, "--type", "update", "--embedded"], out var exitCode);

            exitCode.Should().Be(0);
            output.Should().Contain("ANALYSIS|2|1|0|1|0|UNHEALTHY|extra");
        }
        finally
        {
            File.Delete(logPath);
        }
    }

    [Fact]
    public void Execute_WithMissingLogType_ReturnsError()
    {
        var parser = new FakeLogParser(LogType.Update, []);
        var factory = new FakeLogParserFactory(parser);
        var analyzer = new FakeAnalyzer(new LogStatistics(0, 0, 0, 0, 0, true, ""));

        RunCommand(factory, analyzer, ["analyze", "file.log"], out var exitCode);

        exitCode.Should().Be(1);
    }

    [Fact]
    public void Execute_WithInvalidLogType_ReturnsError()
    {
        var parser = new FakeLogParser(LogType.Update, []);
        var factory = new FakeLogParserFactory(parser);
        var analyzer = new FakeAnalyzer(new LogStatistics(0, 0, 0, 0, 0, true, ""));

        RunCommand(factory, analyzer, ["analyze", "file.log", "--type", "not-a-type"], out var exitCode);

        exitCode.Should().Be(1);
    }

    [Fact]
    public void Execute_WithMissingFileInEmbeddedMode_ReturnsError()
    {
        var parser = new FakeLogParser(LogType.Update, []);
        var factory = new FakeLogParserFactory(parser);
        var analyzer = new FakeAnalyzer(new LogStatistics(0, 0, 0, 0, 0, true, ""));

        RunCommand(factory, analyzer, ["analyze", "missing.log", "--type", "update", "--embedded"], out var exitCode);

        exitCode.Should().Be(1);
    }

    [Fact]
    public void Execute_WithMissingFile_NonEmbedded_ReturnsError()
    {
        var parser = new FakeLogParser(LogType.Update, []);
        var factory = new FakeLogParserFactory(parser);
        var analyzer = new FakeAnalyzer(new LogStatistics(0, 0, 0, 0, 0, true, ""));

        RunCommand(factory, analyzer, ["analyze", "missing.log", "--type", "update"], out var exitCode);

        exitCode.Should().Be(1);
    }

    [Fact]
    public void Execute_NonEmbeddedHealthy_ReturnsSuccess()
    {
        var parser = new FakeLogParser(LogType.Update, []);
        var factory = new FakeLogParserFactory(parser);
        var analyzer = new FakeAnalyzer(new LogStatistics(1, 0, 0, 1, 0, true, ""));

        var logPath = Path.Combine(Path.GetTempPath(), $"analyze_ok_{Guid.NewGuid():N}.log");
        File.WriteAllText(logPath, "content");

        try
        {
            RunCommand(factory, analyzer, ["analyze", logPath, "--type", "update"], out var exitCode);

            exitCode.Should().Be(0);
        }
        finally
        {
            File.Delete(logPath);
        }
    }

    [Fact]
    public void Execute_NonEmbeddedUnhealthy_ReturnsError()
    {
        var parser = new FakeLogParser(LogType.Update, []);
        var factory = new FakeLogParserFactory(parser);
        var analyzer = new FakeAnalyzer(new LogStatistics(1, 1, 0, 0, 0, false, ""));

        var logPath = Path.Combine(Path.GetTempPath(), $"analyze_bad_{Guid.NewGuid():N}.log");
        File.WriteAllText(logPath, "content");

        try
        {
            RunCommand(factory, analyzer, ["analyze", logPath, "--type", "update"], out var exitCode);

            exitCode.Should().Be(1);
        }
        finally
        {
            File.Delete(logPath);
        }
    }

    [Fact]
    public void Execute_WhenFactoryThrows_Embedded_ReturnsError()
    {
        var factory = new ThrowingLogParserFactory();
        var analyzer = new FakeAnalyzer(new LogStatistics(0, 0, 0, 0, 0, true, ""));

        var logPath = Path.Combine(Path.GetTempPath(), $"analyze_throw_{Guid.NewGuid():N}.log");
        File.WriteAllText(logPath, "content");

        try
        {
            RunCommand(factory, analyzer, ["analyze", logPath, "--type", "update", "--embedded"], out var exitCode);

            exitCode.Should().Be(1);
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
        var analyzer = new FakeAnalyzer(new LogStatistics(0, 0, 0, 0, 0, true, ""));

        var logPath = Path.Combine(Path.GetTempPath(), $"analyze_throw_{Guid.NewGuid():N}.log");
        File.WriteAllText(logPath, "content");

        try
        {
            RunCommand(factory, analyzer, ["analyze", logPath, "--type", "update"], out var exitCode);

            exitCode.Should().Be(1);
        }
        finally
        {
            File.Delete(logPath);
        }
    }

    private static string RunCommand(ILogParserFactory factory, ILogAnalyzer analyzer, string[] args, out int exitCode)
    {
        var services = new ServiceCollection();
        services.AddSingleton(factory);
        services.AddSingleton(analyzer);

        var registrar = new TypeRegistrar(services);
        var app = new CommandApp(registrar);
        app.Configure(config => config.AddCommand<AnalyzeCommand>("analyze"));

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

        public ILogParser CreateParser(LogType logType) => _parser;
        public ILogParser CreateParser(LogType logType, StackTraceMode stackTraceMode) => _parser;
        public IEnumerable<LogType> GetAvailableTypes() => new[] { _parser.SupportedLogType };
        public ILogParser GetParserForType(LogType logType) => _parser;
    }

    private sealed class ThrowingLogParserFactory : ILogParserFactory
    {
        public ILogParser CreateParser(LogType logType) => throw new InvalidOperationException("boom");
        public ILogParser CreateParser(LogType logType, StackTraceMode stackTraceMode) => throw new InvalidOperationException("boom");
        public IEnumerable<LogType> GetAvailableTypes() => Array.Empty<LogType>();
        public ILogParser GetParserForType(LogType logType) => throw new InvalidOperationException("boom");
    }

    private sealed class FakeLogParser : ILogParser
    {
        private readonly IReadOnlyList<LogEntry> _entries;

        public FakeLogParser(LogType logType, IReadOnlyList<LogEntry> entries)
        {
            SupportedLogType = logType;
            _entries = entries;
        }

        public LogType SupportedLogType { get; }
        public string ParserName => "Test Parser";
        public string ParserDescription => "Test parser";
        public LogEntry? ParseLine(string line) => null;
        public IEnumerable<LogEntry> ParseFile(string path) => _entries;
        public Task<IEnumerable<LogEntry>> ParseFileAsync(string path)
            => Task.FromResult<IEnumerable<LogEntry>>(_entries);

        public IReadOnlyList<LogColumn> GetColumns() {
            throw new NotImplementedException();
        }
    }

    private sealed class FakeAnalyzer : ILogAnalyzer
    {
        private readonly LogStatistics _stats;

        public FakeAnalyzer(LogStatistics stats)
        {
            _stats = stats;
        }

        public bool HasErrors(IEnumerable<LogEntry> entries) => _stats.ErrorCount > 0;
        public bool HasWarnings(IEnumerable<LogEntry> entries) => _stats.WarningCount > 0;
        public LogStatistics GetStatistics(IEnumerable<LogEntry> entries, LogType type) => _stats;
    }
}
