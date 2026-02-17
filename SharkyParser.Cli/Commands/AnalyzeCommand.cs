using System.ComponentModel;
using SharkyParser.Core.Interfaces;
using SharkyParser.Core.Enums;
using Spectre.Console;
using Spectre.Console.Cli;

namespace SharkyParser.Cli.Commands;

public sealed class AnalyzeCommand(ILogParserFactory parserFactory, ILogAnalyzer analyzer) : Command<AnalyzeCommand.Settings>
{
    public sealed class Settings : CommandSettings
    {
        [CommandArgument(0, "<path>")]
        [Description("Path to the log file (e.g., mylog.log or /path/to/logs/server.log)")]
        public string Path { get; set; } = string.Empty;

        [CommandOption("-t|--type")]
        [Description("Log type: installation, update, rabbitmq, iis")]
        public string? LogTypeString { get; set; }

        [CommandOption("--embedded")]
        [Description("Output in structured format for integration with other tools")]
        public bool Embedded { get; set; }
    }

    protected override int Execute(CommandContext context, Settings settings, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(settings.LogTypeString))
        {
            AnsiConsole.MarkupLine("[red]Error: Log type is required![/]");
            AnsiConsole.WriteLine();
            AnsiConsole.MarkupLine("[yellow]Usage:[/] analyze <file> -t <type>");
            AnsiConsole.WriteLine();
            AnsiConsole.MarkupLine("[cyan]Available log types:[/]");
            AnsiConsole.MarkupLine("  [green]installation[/]  - Installation logs");
            AnsiConsole.MarkupLine("  [green]update[/]        - Update logs");
            AnsiConsole.MarkupLine("  [green]rabbitmq[/]      - RabbitMQ logs");
            AnsiConsole.MarkupLine("  [green]iis[/]           - IIS server logs");
            AnsiConsole.WriteLine();
            AnsiConsole.MarkupLine("[grey]Example: analyze mylog.log -t installation[/]");
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

        if (!File.Exists(settings.Path))
        {
            if (settings.Embedded)
                Console.WriteLine($"ERROR|File not found: {settings.Path}");
            else
                AnsiConsole.MarkupLine($"[red]File not found: {settings.Path}[/]");
            return 1;
        }

        try
        {
            var parser = parserFactory.CreateParser(logType);
            var logs = parser.ParseFile(settings.Path).ToList();
            var stats = analyzer.GetStatistics(logs, logType);

            if (settings.Embedded)
            {
                Console.WriteLine($"ANALYSIS|{stats.TotalCount}|{stats.ErrorCount}|{stats.WarningCount}|{stats.InfoCount}|{stats.DebugCount}|{(stats.IsHealthy ? "HEALTHY" : "UNHEALTHY")}|{stats.ExtendedData}");
                return 0;
            }

            AnsiConsole.MarkupLine($"[blue]Analyzing with {parser.ParserName}:[/] {settings.Path}");
            AnsiConsole.WriteLine();

            var table = new Table()
                .Border(TableBorder.Rounded)
                .AddColumn("[blue]Metric[/]")
                .AddColumn("[blue]Value[/]");

            table.AddRow("Total Entries", stats.TotalCount.ToString());
            table.AddRow("Errors", $"[red]{stats.ErrorCount}[/]");
            table.AddRow("Warnings", $"[yellow]{stats.WarningCount}[/]");
            table.AddRow("Info", $"[green]{stats.InfoCount}[/]");
            table.AddRow("Debug/Trace", $"[grey]{stats.DebugCount}[/]");
            AnsiConsole.WriteLine();
            AnsiConsole.Write(table);

            if (stats.IsHealthy)
            {
                AnsiConsole.MarkupLine("[green]✓ Status: All good![/]");
                return 0;
            }

            AnsiConsole.MarkupLine("[red]⚠  Status: Errors detected![/]");
            return 1;
        }
        catch (Exception ex)
        {
            if (settings.Embedded)
                Console.WriteLine($"ERROR|Analysis failed: {ex.Message}");
            else
                AnsiConsole.MarkupLine($"[red]Analysis failed: {ex.Message}[/]");
            return 1;
        }
    }
}