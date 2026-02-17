using SharkyParser.Core.Interfaces;
using SharkyParser.Core.Models;
using Spectre.Console;

namespace SharkyParser.Cli.Formatters;

/// <summary>
/// Renders parsed log entries as a Spectre.Console table for interactive CLI use.
/// </summary>
public class TableParseFormatter : IParseOutputFormatter
{
    public void Write(IReadOnlyList<LogEntry> logs, ILogParser parser, int totalEntries)
    {
        AnsiConsole.MarkupLine($"[blue]Parser:[/] {parser.ParserName} ({parser.SupportedLogType})");

        if (parser is IConfigurableParser configurable)
            AnsiConsole.MarkupLine($"[blue]{configurable.GetConfigurationSummary()}[/]");

        AnsiConsole.MarkupLine($"[blue]All entries:[/] {totalEntries}");
        AnsiConsole.MarkupLine($"[blue]Filtered entries:[/] {logs.Count}");
        AnsiConsole.WriteLine();

        var dynamicColumns = parser.GetColumns().Where(c => !c.IsPredefined).ToList();

        var table = new Table()
            .Border(TableBorder.Rounded)
            .AddColumn("[blue]Timestamp[/]")
            .AddColumn("[blue]Level[/]")
            .AddColumn("[blue]Message[/]");

        foreach (var column in dynamicColumns)
            table.AddColumn($"[blue]{Markup.Escape(column.Header)}[/]");

        foreach (var log in logs)
        {
            var levelColor = log.Level switch
            {
                "ERROR" => "red",
                "WARN"  => "yellow",
                "INFO"  => "green",
                _       => "grey"
            };

            var rowData = new List<string>
            {
                log.Timestamp.ToString("HH:mm:ss"),
                $"[{levelColor}]{log.Level}[/]",
                Markup.Escape(log.Message)
            };

            foreach (var column in dynamicColumns)
                rowData.Add(log.Fields.TryGetValue(column.Name, out var v) ? Markup.Escape(v) : "");

            table.AddRow(rowData.ToArray());
        }

        AnsiConsole.Write(table);
    }
}
