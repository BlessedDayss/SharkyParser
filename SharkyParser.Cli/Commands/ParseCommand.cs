using System.ComponentModel;
using System.Text;
using SharkyParser.Core;
using SharkyParser.Core.Interfaces;
using SharkyParser.Core.Enums;
using SharkyParser.Core.Models;
using SharkyParser.Core.Parsers;
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
        [Description("Log type: installation, update, rabbitmq, iis")]
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
            AnsiConsole.MarkupLine("[red]Error: Log type is required![/]");
            AnsiConsole.WriteLine();
            AnsiConsole.MarkupLine("[yellow]Usage:[/] parse <file> -t <type>");
            AnsiConsole.WriteLine();
            AnsiConsole.MarkupLine("[cyan]Available log types:[/]");
            AnsiConsole.MarkupLine("  [green]installation[/]  - Installation logs");
            AnsiConsole.MarkupLine("  [green]update[/]        - Update logs");
            AnsiConsole.MarkupLine("  [green]rabbitmq[/]      - RabbitMQ logs");
            AnsiConsole.MarkupLine("  [green]iis[/]           - IIS server logs");
            AnsiConsole.WriteLine();
            AnsiConsole.MarkupLine("[grey]Example: parse mylog.log -t installation[/]");
            return 1;
        }
        
        if (!Enum.TryParse<LogType>(settings.LogTypeString, true, out var logType))
        {
            AnsiConsole.MarkupLine($"[red]Error: Unknown log type '{settings.LogTypeString}'[/]");
            AnsiConsole.WriteLine();
            AnsiConsole.MarkupLine("[cyan]Available log types:[/]");
            AnsiConsole.MarkupLine("  [green]installation[/]  - Installation logs");
            AnsiConsole.MarkupLine("  [green]update[/]        - Update logs");
            AnsiConsole.MarkupLine("  [green]rabbitmq[/]      - RabbitMQ logs");
            AnsiConsole.MarkupLine("  [green]iis[/]           - IIS server logs");
            return 1;
        }

        StackTraceMode stackTraceMode = StackTraceMode.AllToStackTrace;
        if (logType == LogType.Installation && !settings.Embedded)
        {
            stackTraceMode = PromptStackTraceMode();
        }


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
            var totalEntries = allLogs.Count;
            
            var logs = allLogs;
            if (!string.IsNullOrWhiteSpace(settings.Filter))
            {
                var filterLevel = settings.Filter.ToUpperInvariant();
                logs = logs.Where(l => l.Level.Equals(filterLevel, StringComparison.OrdinalIgnoreCase)).ToList();
            }

            if (settings.Embedded)
            {
                OutputEmbeddedFormat(logs, parser, totalEntries); 
            }
            else
            {
                OutputTableFormat(logs, parser, totalEntries);
            }
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

    private static StackTraceMode PromptStackTraceMode()
    {
        AnsiConsole.MarkupLine("[blue]Choose stack trace parsing mode:[/]");

        var mode = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title("How should stack traces be handled?")
                .PageSize(10)
                .AddChoices(new[] {
                    "All lines after timestamp go to stack trace",
                    "No stack traces - all in message"
                }));

        return mode switch
        {
            "All lines after timestamp go to stack trace" => StackTraceMode.AllToStackTrace,
            "No stack traces - all in message" => StackTraceMode.NoStackTrace,
            _ => StackTraceMode.AllToStackTrace
        };
    }
    
    private static void OutputEmbeddedFormat(List<LogEntry> logs, ILogParser parser, int totalEntries)
    {
        // Calculate statistics by level
        var errors = logs.Count(l => l.Level.Equals("ERROR", StringComparison.OrdinalIgnoreCase));
        var warnings = logs.Count(l => l.Level.Equals("WARN", StringComparison.OrdinalIgnoreCase) || l.Level.Equals("WARNING", StringComparison.OrdinalIgnoreCase));
        var info = logs.Count(l => l.Level.Equals("INFO", StringComparison.OrdinalIgnoreCase));
        var debug = logs.Count(l => l.Level.Equals("DEBUG", StringComparison.OrdinalIgnoreCase) || l.Level.Equals("TRACE", StringComparison.OrdinalIgnoreCase));
        
        // Statistics line: STATS|total|errors|warnings|info|debug
        Console.WriteLine($"STATS|{totalEntries}|{errors}|{warnings}|{info}|{debug}");

        // Get dynamic columns from parser
        var dynamicColumns = parser.GetColumns().Where(c => !c.IsPredefined).ToList();
        
        // Output column headers if there are dynamic columns
        if (dynamicColumns.Any())
        {
            var headerLine = new StringBuilder("COLUMNS|");
            headerLine.Append(string.Join("|", dynamicColumns.Select(c => EscapePipe(c.Name))));
            Console.WriteLine(headerLine.ToString());
        }

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
            line.Append(log.LineNumber);
            line.Append('|');
            line.Append(EscapePipe(log.FilePath));
            line.Append('|');
            line.Append(EscapePipe(log.RawData));
            
            // Append dynamic field values
            foreach (var column in dynamicColumns)
            {
                line.Append('|');
                if (log.Fields.TryGetValue(column.Name, out var value))
                {
                    line.Append(EscapePipe(value));
                }
            }
            
            Console.WriteLine(line.ToString());
        }
    }

    private static void OutputTableFormat(List<LogEntry> logs, ILogParser parser, int totalEntries)
    {
        AnsiConsole.MarkupLine($"[blue]Parser:[/] {parser.ParserName} ({parser.SupportedLogType})");
        if (parser is InstallationLogParser installationParser)
        {
            AnsiConsole.MarkupLine($"[blue]Stack Trace Mode:[/] {installationParser.StackTraceMode}");
        }
        AnsiConsole.MarkupLine($"[blue]All entries:[/] {totalEntries}");
        AnsiConsole.MarkupLine($"[blue]Filtered entries:[/] {logs.Count}");
        AnsiConsole.WriteLine();

        // Get dynamic columns from parser
        var dynamicColumns = parser.GetColumns().Where(c => !c.IsPredefined).ToList();

        var table = new Table()
            .Border(TableBorder.Rounded)
            .AddColumn("[blue]Timestamp[/]")
            .AddColumn("[blue]Level[/]")
            .AddColumn("[blue]Message[/]");

        // Add dynamic columns to table
        foreach (var column in dynamicColumns)
        {
            table.AddColumn($"[blue]{Markup.Escape(column.Header)}[/]");
        }

        foreach (var log in logs)
        {
            var levelColor = log.Level switch
            {
                "ERROR" => "red",
                "WARN" => "yellow",
                "INFO" => "green",
                _ => "grey"
            };

            var rowData = new List<string>
            {
                log.Timestamp.ToString("HH:mm:ss"),
                $"[{levelColor}]{log.Level}[/]",
                Markup.Escape(log.Message)
            };
            
            // Add dynamic field values to row
            foreach (var column in dynamicColumns)
            {
                if (log.Fields.TryGetValue(column.Name, out var value))
                {
                    rowData.Add(Markup.Escape(value));
                }
                else
                {
                    rowData.Add("");
                }
            }

            table.AddRow(rowData.ToArray());
        }

        AnsiConsole.Write(table);
    }

    private static string EscapePipe(string? value)
    {
        if (string.IsNullOrEmpty(value)) return "";
        return value.Replace("|", "\\|").Replace("\n", "\\n").Replace("\r", "");
    }
}
