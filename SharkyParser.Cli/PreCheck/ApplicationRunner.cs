using SharkyParser.Cli.Interfaces;
using SharkyParser.Cli.PreCheck;

namespace SharkyParser.Cli;

public class ApplicationRunner(
    IApplicationModeDetector modeDetector,
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
            ApplicationMode.Cli         => cliRunner.Run(args),
            ApplicationMode.Interactive => interactiveRunner.Run(),
            ApplicationMode.Embedded    => embeddedRunner.Run(args),
            _ => throw new InvalidOperationException($"Unknown application mode: {mode}")
        };
    }
}
