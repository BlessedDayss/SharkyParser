using SharkyParser.Core.Interfaces;
using Spectre.Console.Cli;

namespace SharkyParser.Cli.PreCheck;

public class CliModeRunner(CommandApp app, IAppLogger logger)
{
    private readonly CommandApp _app = app;
    private readonly IAppLogger _logger = logger;
    public int Run(string[] args)
    {
        logger.LogInfo($"CLI mode: executing command '{string.Join(" ", args)}'");
        var result = _app.Run(args);
        _logger.LogCommandExecution(string.Join(" ", args), result);
        return result;
    }
}