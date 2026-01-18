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
    private readonly ApplicationModeDetector _modeDetector = modeDetector;
    private readonly ICliModeRunner _cliRunner = cliRunner;
    private readonly IInteractiveModeRunner _interactiveRunner = interactiveRunner;
    private readonly IEmbeddedModeRunner _embeddedRunner = embeddedRunner;
    private readonly IAppLogger _logger = logger;

    public int Run(string[] args)
    {
        _logger.LogAppStart(args);
        var mode = _modeDetector.DetermineMode(args);
        _logger.LogModeDetected(mode.ToString());
        
        return mode switch
        {
            ApplicationMode.Cli => _cliRunner.Run(args),
            ApplicationMode.Interactive => _interactiveRunner.Run(),
            ApplicationMode.Embedded => _embeddedRunner.Run(args),
            _ => throw new InvalidOperationException($"Unknown application mode: {mode}")
        };
    }
}