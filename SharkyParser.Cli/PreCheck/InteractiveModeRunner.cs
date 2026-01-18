using SharkyParser.Cli.UI;
using SharkyParser.Core.Interfaces;
using Spectre.Console;
using Spectre.Console.Cli;

namespace SharkyParser.Cli.PreCheck;

public interface IInteractiveModeRunner 
{ 
    int Run(); 
}

public class InteractiveModeRunner(CommandApp app, IAppLogger logger) : IInteractiveModeRunner
{
    private readonly CommandApp _app = app;
    private readonly IAppLogger _logger = logger;

    public int Run()
    {
        SpinnerLoader.ShowStartup();
        BannerRenderer.Show();
        TipsRenderer.Show();
        
        var history = new CommandHistory();

        while (true)
        {
            var input = InputReader.ReadLineWithHistory(history, "> ");
            
            if(string.IsNullOrWhiteSpace(input))
                continue;

            var command = input.Trim().ToLower();

            if (command is "exit" or "quit")
            {
                AnsiConsole.MarkupLine("[grey]Goodbye![/]");
                break;
            }
            
            if (command is "/help" or "help" or "?")
            {
                _app.Run(["--help"]);
                continue;
            }
            
            var commandArgs = input.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            _app.Run(commandArgs);
        }
        return 0;
    }
}