using Spectre.Console;

namespace SharkyParser.Cli.UI;

public static class TipsRenderer
{
    public static void Show()
    {
        AnsiConsole.MarkupLine("[grey]Tips for getting started:[/]");
        AnsiConsole.MarkupLine("[grey]1. parse <file> - parse logs[/]");
        AnsiConsole.MarkupLine("[grey]2. analyze <file> - check status[/]");
        AnsiConsole.MarkupLine("[grey]3. /help - show all commands[/]");
        AnsiConsole.MarkupLine("[grey]4. exit - quit[/]");
        AnsiConsole.WriteLine();
    }
}
