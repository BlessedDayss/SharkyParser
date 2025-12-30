using System.ComponentModel;
using SharkyParser.Core.Interfaces;
using Spectre.Console;
using Spectre.Console.Cli;

namespace SharkyParser.Cli.Commands;

public sealed class AnalyzeCommand : Command<AnalyzeCommand.Settings>
{
    private readonly ILogParser _parser;
    private readonly ILogAnalyzer _analyzer;

    public AnalyzeCommand(ILogParser parser, ILogAnalyzer analyzer)
    {
        _parser = parser;
        _analyzer = analyzer;
    }

    public sealed class Settings : CommandSettings
    {
        [CommandArgument(0, "<path>")]
        [Description("Path to the log file to analyze")]
        public string Path { get; set; } = string.Empty;
    }

    protected override int Execute(CommandContext context, Settings settings, CancellationToken cancellationToken)
    {
        if (!File.Exists(settings.Path))
        {
            AnsiConsole.MarkupLine($"[red]File not found: {settings.Path}[/]");
            return 1;
        }

        var logs = _parser.ParseFile(settings.Path);
        var stats = _analyzer.GetStatistics(logs);

        AnsiConsole.MarkupLine($"[blue]Analyzing:[/] {settings.Path}");
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
            AnsiConsole.MarkupLine("[green]✓ Health Status: HEALTHY[/]");
            return 0;
        }

        AnsiConsole.MarkupLine("[red]⚠ Health Status: UNHEALTHY - Errors detected[/]");
        return 1;
    }
}
