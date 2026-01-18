using System.ComponentModel;
using System.Text;
using SharkyParser.Core.Interfaces;
using Spectre.Console;
using Spectre.Console.Cli;

namespace SharkyParser.Cli.Commands;

public sealed class ParseCommand : Command<ParseCommand.Settings>
{
    private readonly ILogParser _parser;

    public ParseCommand(ILogParser parser)
    {
        _parser = parser;
    }

    public sealed class Settings : CommandSettings
    {
        [CommandArgument(0, "<path>")]
        [Description("Path to the log file to parse")]
        public string Path { get; set; } = string.Empty;

        [CommandOption("--json")]
        [Description("Output in JSON format")]
        public bool Json { get; set; }

        [CommandOption("--embedded")]
        [Description("Output in pipe-delimited format for embedded use (fastest)")]
        public bool Embedded { get; set; }

        [CommandOption("-f|--filter")]
        [Description("Filter by log level (error, warn, info, debug)")]
        public string? Filter { get; set; }
    }

    protected override int Execute(CommandContext context, Settings settings, CancellationToken cancellationToken)
    {
        if (!File.Exists(settings.Path))
        {
            if (settings.Embedded)
                Console.WriteLine("ERROR|File not found");
            else
                AnsiConsole.MarkupLine($"[red]File not found: {settings.Path}[/]");
            return 1;
        }

        var logs = _parser.ParseFile(settings.Path).ToList();

        // Apply filter if specified
        if (!string.IsNullOrWhiteSpace(settings.Filter))
        {
            var filterLevel = settings.Filter.ToUpperInvariant();
            logs = logs.Where(l => l.Level.Equals(filterLevel, StringComparison.OrdinalIgnoreCase)).ToList();

            if (logs.Count == 0 && !settings.Embedded)
            {
                AnsiConsole.MarkupLine($"[yellow]No logs found with level: {settings.Filter}[/]");
                return 0;
            }
        }

        // Embedded format (pipe-delimited) - fastest for IPC
        if (settings.Embedded)
        {
            OutputEmbeddedFormat(logs);
            return 0;
        }

        // JSON format
        if (settings.Json)
        {
            OutputJsonFormat(logs);
            return 0;
        }

        // Table format (default)
        OutputTableFormat(logs);
        return 0;
    }

    private static void OutputEmbeddedFormat(List<SharkyParser.Core.LogEntry> logs)
    {
        // Statistics line first
        var stats = new StringBuilder();
        stats.Append("STATS|");
        stats.Append(logs.Count);
        stats.Append('|');
        stats.Append(logs.Count(l => l.Level == "ERROR"));
        stats.Append('|');
        stats.Append(logs.Count(l => l.Level == "WARN"));
        stats.Append('|');
        stats.Append(logs.Count(l => l.Level == "INFO"));
        stats.Append('|');
        stats.Append(logs.Count(l => l.Level == "DEBUG"));
        Console.WriteLine(stats.ToString());

        // Entry lines
        foreach (var log in logs)
        {
            var line = new StringBuilder();
            line.Append("ENTRY|");
            line.Append(log.Timestamp.ToString("o"));
            line.Append('|');
            line.Append(log.Level);
            line.Append('|');
            line.Append(EscapePipe(log.Message));
            line.Append('|');
            line.Append(EscapePipe(log.Source));
            line.Append('|');
            line.Append(EscapePipe(log.StackTrace));
            line.Append('|');
            line.Append(log.LineNumber);
            line.Append('|');
            line.Append(EscapePipe(log.FilePath));
            line.Append('|');
            line.Append(EscapePipe(log.RawData));
            Console.WriteLine(line.ToString());
        }
    }

    private static string EscapePipe(string? value)
    {
        if (string.IsNullOrEmpty(value)) return "";
        return value.Replace("|", "\\|").Replace("\n", "\\n").Replace("\r", "");
    }

    private static void OutputJsonFormat(List<SharkyParser.Core.LogEntry> logs)
    {
        var result = new
        {
            entries = logs.Select(l => new
            {
                timestamp = l.Timestamp,
                level = l.Level,
                message = l.Message,
                source = l.Source,
                stackTrace = l.StackTrace,
                lineNumber = l.LineNumber,
                filePath = l.FilePath,
                rawData = l.RawData
            }),
            statistics = new
            {
                total = logs.Count,
                errors = logs.Count(l => l.Level == "ERROR"),
                warnings = logs.Count(l => l.Level == "WARN"),
                info = logs.Count(l => l.Level == "INFO"),
                debug = logs.Count(l => l.Level == "DEBUG")
            }
        };
        Console.WriteLine(System.Text.Json.JsonSerializer.Serialize(result, new System.Text.Json.JsonSerializerOptions { WriteIndented = false }));
    }

    private static void OutputTableFormat(List<SharkyParser.Core.LogEntry> logs)
    {
        var table = new Table()
            .Border(TableBorder.Rounded)
            .AddColumn("[blue]Timestamp[/]")
            .AddColumn("[blue]Level[/]")
            .AddColumn("[blue]Message[/]");

        foreach (var log in logs)
        {
            var levelColor = log.Level switch
            {
                "ERROR" => "red",
                "WARN" => "yellow",
                "INFO" => "green",
                _ => "grey"
            };

            table.AddRow(
                log.Timestamp.ToString("HH:mm:ss"),
                $"[{levelColor}]{log.Level}[/]",
                Markup.Escape(log.Message)
            );
        }

        AnsiConsole.Write(table);
    }
}
