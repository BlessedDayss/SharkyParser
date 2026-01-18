using Spectre.Console;

namespace SharkyParser.Cli.UI;

public static class SpinnerLoader
{
    public static void ShowStartup()
    {
        AnsiConsole.Status()
            .Spinner(Spinner.Known.Binary)
            .SpinnerStyle(Style.Parse("blue"))
            .Start("[blue]Starting Sharky Parser...[/]", _ =>
            {
                Thread.Sleep(3000);
            });
    }
}
