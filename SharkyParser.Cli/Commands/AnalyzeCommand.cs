using System.ComponentModel;
using SharkyParser.Cli.Formatters;
using SharkyParser.Core;
using SharkyParser.Core.Enums;
using SharkyParser.Core.Interfaces;
using Spectre.Console;
using Spectre.Console.Cli;

namespace SharkyParser.Cli.Commands;

public sealed class AnalyzeCommand(ILogParserFactory parserFactory, ILogAnalyzer analyzer)
    : Command<AnalyzeCommand.Settings>
{
    public sealed class Settings : CommandSettings
    {
        [CommandArgument(0, "<path>")]
        [Description("Path to the log file (e.g., mylog.log or /path/to/logs/server.log)")]
        public string Path { get; set; } = string.Empty;

        [CommandOption("-t|--type")]
        [Description("Log type: installation, update, rabbitmq, iis, teamcity")]
        public string? LogTypeString { get; set; }

        [CommandOption("--embedded")]
        [Description("Output in structured format for integration with other tools")]
        public bool Embedded { get; set; }
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

            IAnalyzeOutputFormatter formatter = settings.Embedded
                ? new EmbeddedAnalyzeFormatter()
                : new TableAnalyzeFormatter();

            formatter.Write(stats, parser.ParserName, settings.Path);

            // In embedded mode the consumer reads HEALTHY/UNHEALTHY from the output line.
            // In interactive (table) mode the exit code signals health to the shell.
            return settings.Embedded || stats.IsHealthy ? 0 : 1;
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

    // ── Helpers ──────────────────────────────────────────────────────────────

    private static void PrintUsageError(bool embedded)
    {
        if (embedded) { Console.WriteLine("ERROR|Log type is required"); return; }
        AnsiConsole.MarkupLine("[red]Error: Log type is required![/]");
        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine("[yellow]Usage:[/] analyze <file> -t <type>");
        PrintAvailableTypes();
        AnsiConsole.MarkupLine("[grey]Example: analyze mylog.log -t installation[/]");
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
