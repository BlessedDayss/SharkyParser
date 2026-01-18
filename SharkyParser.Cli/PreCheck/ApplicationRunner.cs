using SharkyParser.Cli.PreCheck;
using SharkyParser.Core.Interfaces;

namespace SharkyParser.Cli;

public class ApplicationRunner(
    ApplicationModeDetector modeDetector,
    CliModeRunner cliRunner,
    InteractiveModeRunner interactiveRunner,
    EmbeddedModeRunner embeddedRunner,IAppLogger logger)
{
    private readonly ApplicationModeDetector _modeDetector = modeDetector;
    private readonly CliModeRunner _cliRunner = cliRunner;
    private readonly InteractiveModeRunner _interactiveRunner = interactiveRunner;
    private readonly EmbeddedModeRunner _embeddedRunner = embeddedRunner;

    public int Run(string[] args)
    {
        logger.LogAppStart(args);
        var mode = _modeDetector.DetermineMode(args);
        logger.LogModeDetected(mode.ToString());
        
        return mode switch
        {
            ApplicationMode.Cli => _cliRunner.Run(args),
            ApplicationMode.Interactive => _interactiveRunner.Run(),
            ApplicationMode.Embedded => _embeddedRunner.Run(args),
            _ => throw new InvalidOperationException($"Unknown application mode: {mode}")
        };
    }
}