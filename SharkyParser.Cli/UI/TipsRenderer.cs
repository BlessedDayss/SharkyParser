using Spectre.Console;

namespace SharkyParser.Cli.UI;

public static class TipsRenderer
{
    public static void Show()
    {
        var panel = new Panel(
            new Markup(
                "[bold cyan]Available Commands:[/]\n\n" +
                "[green]parse[/] <file> -t <type>     Parse and display log entries\n" +
                "[green]analyze[/] <file> -t <type>  Analyze log and show statistics\n\n" +
                "[bold cyan]Log Types (-t option):[/]\n\n" +
                "  [yellow]installation[/]  Installation logs\n" +
                "  [yellow]update[/]        Update logs\n" +
                "  [yellow]rabbitmq[/]      RabbitMQ logs\n" +
                "  [yellow]iis[/]           IIS server logs\n\n" +
                "[bold cyan]Examples:[/]\n\n" +
                "  [grey]parse mylog.log -t installation[/]\n" +
                "  [grey]analyze server.log -t iis[/]\n" +
                "  [grey]parse update.log -t update -f error[/]\n\n" +
                "[grey italic]Type [white]/help[/] for detailed help, [white]exit[/] to quit[/]"
            ))
            .Header("[bold aqua]Quick Start Guide[/]")
            .Border(BoxBorder.Rounded)
            .BorderColor(Color.Grey)
            .Padding(1, 0);
        
        AnsiConsole.Write(panel);
        AnsiConsole.WriteLine();
    }
}
