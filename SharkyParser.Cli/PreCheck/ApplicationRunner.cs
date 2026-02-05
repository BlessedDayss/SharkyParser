using SharkyParser.Cli.PreCheck;
using SharkyParser.Core.Interfaces;

namespace SharkyParser.Cli;

public class ApplicationRunner(
    ApplicationModeDetector modeDetector,
    ICliModeRunner cliRunner,
    IInteractiveModeRunner interactiveRunner,
    IEmbeddedModeRunner embeddedRunner,
    IAppLogger logger)
{


    public int Run(string[] args)
    {
        logger.LogAppStart(args);
        var mode = modeDetector.DetermineMode(args);
        logger.LogModeDetected(mode.ToString());
        
        return mode switch
        {
            ApplicationMode.Cli => cliRunner.Run(args),
            ApplicationMode.Interactive => interactiveRunner.Run(),
            ApplicationMode.Embedded => embeddedRunner.Run(args),
            _ => throw new InvalidOperationException($"Unknown application mode: {mode}")
        };
    }
}