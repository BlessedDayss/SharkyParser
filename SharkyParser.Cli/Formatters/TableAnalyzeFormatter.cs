using SharkyParser.Core;
using Spectre.Console;

namespace SharkyParser.Cli.Formatters;

/// <summary>
/// Renders analysis results as a Spectre.Console table for interactive CLI use.
/// Returns the health status via the exit code convention: call Write, then check stats.IsHealthy.
/// </summary>
public class TableAnalyzeFormatter : IAnalyzeOutputFormatter
{
    public void Write(LogStatistics stats, string parserName, string filePath)
    {
        AnsiConsole.MarkupLine($"[blue]Analyzing with {parserName}:[/] {filePath}");
        AnsiConsole.WriteLine();

        var table = new Table()
            .Border(TableBorder.Rounded)
            .AddColumn("[blue]Metric[/]")
            .AddColumn("[blue]Value[/]");

        table.AddRow("Total Entries", stats.TotalCount.ToString());
        table.AddRow("Errors",        $"[red]{stats.ErrorCount}[/]");
        table.AddRow("Warnings",      $"[yellow]{stats.WarningCount}[/]");
        table.AddRow("Info",          $"[green]{stats.InfoCount}[/]");
        table.AddRow("Debug/Trace",   $"[grey]{stats.DebugCount}[/]");

        AnsiConsole.WriteLine();
        AnsiConsole.Write(table);

        if (stats.IsHealthy)
            AnsiConsole.MarkupLine("[green]✓ Status: All good![/]");
        else
            AnsiConsole.MarkupLine("[red]⚠  Status: Errors detected![/]");
    }
}
