using System.ComponentModel;
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

        var logs = _parser.ParseFile(settings.Path).ToList();

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

        if (settings.Json)
        {
            foreach (var log in logs)
            {
                AnsiConsole.WriteLine(System.Text.Json.JsonSerializer.Serialize(log));
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
