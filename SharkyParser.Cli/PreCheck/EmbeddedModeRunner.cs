using SharkyParser.Core.Interfaces;
using Spectre.Console.Cli;

namespace SharkyParser.Cli.PreCheck;

public interface IEmbeddedModeRunner 
{ 
    int Run(string[] args); 
}

public class EmbeddedModeRunner(CommandApp app, IAppLogger logger) : IEmbeddedModeRunner
{
    private readonly CommandApp _app = app;
    private readonly IAppLogger _logger = logger;
    
    public int Run(string[] args)
    {
        _logger.LogInfo($"Embedded mode: executing command '{string.Join(" ", args)}'");
        var result = _app.Run(args);
        _logger.LogCommandExecution(string.Join(" ", args), result);
        return result;
    }
}