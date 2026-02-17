using SharkyParser.Core.Interfaces;

namespace SharkyParser.Cli.Interfaces;

/// <summary>
/// CLI-specific application lifecycle logger.
/// Extends ILogger (Core) with host-specific events that have no meaning
/// outside the CLI host: startup args, mode detection, command execution.
/// Core services depend on ILogger only — not on this interface.
/// </summary>
public interface IAppLogger : ILogger
{
    void LogAppStart(string[] args);
    void LogModeDetected(string mode);
    void LogFileProcessed(string filePath);
    void LogCommandExecution(string command, int exitCode);
}
