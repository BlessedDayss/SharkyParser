using Spectre.Console;

namespace SharkyParser.Cli.UI;

/// <summary>
/// Displays helpful tips for getting started with the CLI.
/// </summary>
public static class TipsRenderer
{
    public static void Show()
    {
        AnsiConsole.MarkupLine("[grey]Tips for getting started:[/]");
        AnsiConsole.MarkupLine("[grey]1. Use 'sharky parse <file>' to parse logs[/]");
        AnsiConsole.MarkupLine("[grey]2. Use 'sharky analyze <file>' to check health[/]");
        AnsiConsole.MarkupLine("[grey]3. /help for more information.[/]");
        AnsiConsole.WriteLine();
    }
}
