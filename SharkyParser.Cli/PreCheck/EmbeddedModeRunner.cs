using SharkyParser.Core.Interfaces;
using Spectre.Console.Cli;

namespace SharkyParser.Cli.PreCheck;

public interface IEmbeddedModeRunner 
{ 
    int Run(string[] args); 
}

public class EmbeddedModeRunner(CommandApp app, IAppLogger logger) : IEmbeddedModeRunner
{

    public int Run(string[] args)
    {
        logger.LogInfo($"Embedded mode: executing command '{string.Join(" ", args)}'");
        var result = app.Run(args);
        logger.LogCommandExecution(string.Join(" ", args), result);
        return result;
    }
}