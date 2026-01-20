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
        [Description("Path to the log file to analyze")]
        public string Path { get; set; } = string.Empty;

        [CommandOption("-t|--type")]
        [Description("REQUIRED: Log type - installation, update, rabbit, iis")]
        public string? LogTypeString { get; set; }

        [CommandOption("--embedded")]
        [Description("Output in structured format for embedded use")]
        public bool Embedded { get; set; }
    }

    protected override int Execute(CommandContext context, Settings settings, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(settings.LogTypeString))
        {
            AnsiConsole.MarkupLine("[red]Error: Log type is required. Use --type option.[/]");
            return 1;
        }
        if (!Enum.TryParse<LogType>(settings.LogTypeString, true, out var logType))
        {
            AnsiConsole.MarkupLine("[red]Error: Log type is required. Use --type option.[/]");
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
            var stats = analyzer.GetStatistics(logs);

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