using Spectre.Console;

namespace SharkyParser.Cli.UI;

/// <summary>
/// Displays loading spinner animation during startup.
/// </summary>
public static class SpinnerLoader
{
    public static void ShowStartup()
    {
        AnsiConsole.Status()
            .Spinner(Spinner.Known.Binary)
            .SpinnerStyle(Style.Parse("blue"))
            .Start("[blue]Starting Sharky Parser...[/]", ctx =>
            {
                Thread.Sleep(500);
            });
    }
}
