using System.ComponentModel;
using SharkyParser.Core.Interfaces;
using Spectre.Console;
using Spectre.Console.Cli;

namespace SharkyParser.Cli.Commands;

public sealed class ParseCommand(ILogParser parser) : Command<ParseCommand.Settings>
{
    public class Settings : CommandSettings
    {
        [CommandArgument(0, "<path>")]
        [Description("Path to the log file to parse")]
        public string Path { get; set; } = string.Empty;
        
        //TODO: Enable JSON output in future versions
        // [CommandOption("--json")]
        // [Description("Output in JSON format (for CLI usage)")]
        // public bool Json { get; set; }

        [CommandOption("--embedded")]
        [Description("Output optimized for embedded usage (default)")]
        public bool Embedded { get; set; }

        [CommandOption("-f|--filter")]
        [Description("Filter by log level (error, warn, info, debug)")]
        public string? Filter { get; set; }
    }

    protected override int Execute(CommandContext context, Settings settings, CancellationToken cancellationToken)
    {
        if (!File.Exists(settings.Path))
        {
            AnsiConsole.MarkupLine($"[red]File not found: {settings.Path}[/]");
            return 1;
        }

        var logs = parser.ParseFile(settings.Path).ToList();

        // Apply filter if specified
        if (!string.IsNullOrWhiteSpace(settings.Filter))
        {
            var filterLevel = settings.Filter.ToUpperInvariant();
            logs = logs.Where(l => l.Level.Equals(filterLevel, StringComparison.OrdinalIgnoreCase)).ToList();

            if (logs.Count == 0)
            {
                AnsiConsole.MarkupLine($"[yellow]No logs found with level: {settings.Filter}[/]");
                return 0;
            }
        }

        if (settings.Embedded)
        {
            // Optimized output for embedded usage - direct field output
            AnsiConsole.MarkupLine($"STATS|{logs.Count}|{logs.Count(l => l.Level == "ERROR")}|{logs.Count(l => l.Level == "WARN")}|{logs.Count(l => l.Level == "INFO")}|{logs.Count(l => l.Level == "DEBUG")}");

            foreach (var log in logs)
            {
                // Format: ENTRY|timestamp|level|message|source|stackTrace|lineNumber|filePath|rawData
                AnsiConsole.MarkupLine($"ENTRY|{log.Timestamp:o}|{log.Level}|{log.Message.Replace("|", "\\|")}|{log.Source}|{log.StackTrace?.Replace("|", "\\|")}|{log.LineNumber}|{log.FilePath}|{log.RawData?.Replace("|", "\\|")}");
            }
        }
        else
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
        return 0;
    }
}
