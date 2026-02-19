using System.ComponentModel;
using SharkyParser.Cli.Formatters;
using SharkyParser.Core.Enums;
using SharkyParser.Core.Interfaces;
using SharkyParser.Core.Models;
using Spectre.Console;
using Spectre.Console.Cli;

namespace SharkyParser.Cli.Commands;

public sealed class ParseCommand(ILogParserFactory parserFactory) : Command<ParseCommand.Settings>
{
    public class Settings : CommandSettings
    {
        [CommandArgument(0, "<path>")]
        [Description("Path to the log file (e.g., mylog.log or /path/to/logs/server.log)")]
        public string Path { get; set; } = string.Empty;

        [CommandOption("-t|--type")]
        [Description("Log type: installation, update, rabbitmq, iis, teamcity")]
        public string? LogTypeString { get; set; }

        [CommandOption("--embedded")]
        [Description("Output in pipe-delimited format for integration with other tools")]
        public bool Embedded { get; set; }

        [CommandOption("-f|--filter")]
        [Description("Filter by log level: error, warn, info, debug (shows only matching entries)")]
        public string? Filter { get; set; }
    }

    protected override int Execute(CommandContext context, Settings settings, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(settings.LogTypeString))
        {
            PrintUsageError(settings.Embedded);
            return 1;
        }

        if (!Enum.TryParse<LogType>(settings.LogTypeString, true, out var logType))
        {
            PrintUnknownTypeError(settings.LogTypeString, settings.Embedded);
            return 1;
        }

        var stackTraceMode = StackTraceMode.AllToStackTrace;
        if (logType == LogType.Installation && !settings.Embedded)
            stackTraceMode = PromptStackTraceMode();

        if (!File.Exists(settings.Path))
        {
            if (settings.Embedded)
                Console.WriteLine("ERROR|File not found");
            else
                AnsiConsole.MarkupLine($"[red]File not found: {settings.Path}[/]");
            return 1;
        }

        try
        {
            var parser = parserFactory.CreateParser(logType, stackTraceMode);
            var allLogs = parser.ParseFile(settings.Path).ToList();
            var filteredLogs = ApplyFilter(allLogs, settings.Filter);

            IParseOutputFormatter formatter = settings.Embedded
                ? new EmbeddedParseFormatter()
                : new TableParseFormatter();

            formatter.Write(filteredLogs, parser, allLogs.Count);
        }
        catch (Exception ex)
        {
            if (settings.Embedded)
                Console.WriteLine($"ERROR|Parser error: {ex.Message}");
            else
                AnsiConsole.MarkupLine($"[red]Parser error: {ex.Message}[/]");
            return 1;
        }

        return 0;
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private static List<LogEntry> ApplyFilter(List<LogEntry> logs, string? filter)
    {
        if (string.IsNullOrWhiteSpace(filter))
            return logs;

        var level = filter.ToUpperInvariant();
        return logs.Where(l => l.Level.Equals(level, StringComparison.OrdinalIgnoreCase)).ToList();
    }

    private static StackTraceMode PromptStackTraceMode()
    {
        AnsiConsole.MarkupLine("[blue]Choose stack trace parsing mode:[/]");

        var mode = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title("How should stack traces be handled?")
                .PageSize(10)
                .AddChoices(
                    "All lines after timestamp go to stack trace",
                    "No stack traces - all in message"));

        return mode == "All lines after timestamp go to stack trace"
            ? StackTraceMode.AllToStackTrace
            : StackTraceMode.NoStackTrace;
    }

    private static void PrintUsageError(bool embedded)
    {
        if (embedded) { Console.WriteLine("ERROR|Log type is required"); return; }
        AnsiConsole.MarkupLine("[red]Error: Log type is required![/]");
        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine("[yellow]Usage:[/] parse <file> -t <type>");
        PrintAvailableTypes();
        AnsiConsole.MarkupLine("[grey]Example: parse mylog.log -t installation[/]");
    }

    private static void PrintUnknownTypeError(string type, bool embedded)
    {
        if (embedded) { Console.WriteLine($"ERROR|Unknown log type '{type}'"); return; }
        AnsiConsole.MarkupLine($"[red]Error: Unknown log type '{type}'[/]");
        AnsiConsole.WriteLine();
        PrintAvailableTypes();
    }

    private static void PrintAvailableTypes()
    {
        AnsiConsole.MarkupLine("[cyan]Available log types:[/]");
        AnsiConsole.MarkupLine("  [green]installation[/]  - Installation logs");
        AnsiConsole.MarkupLine("  [green]update[/]        - Update logs");
        AnsiConsole.MarkupLine("  [green]rabbitmq[/]      - RabbitMQ logs");
        AnsiConsole.MarkupLine("  [green]iis[/]           - IIS server logs");
        AnsiConsole.MarkupLine("  [green]teamcity[/]      - TeamCity build logs");
        AnsiConsole.WriteLine();
    }
}
