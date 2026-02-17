using SharkyParser.Cli.Interfaces;
using Spectre.Console.Cli;

namespace SharkyParser.Cli.PreCheck;

public class CliModeRunner(CommandApp app, IAppLogger logger) : ICliModeRunner
{
    public int Run(string[] args)
    {
        logger.LogInfo($"CLI mode: executing command '{string.Join(" ", args)}'");
        var result = app.Run(args);
        logger.LogCommandExecution(string.Join(" ", args), result);
        return result;
    }
}
