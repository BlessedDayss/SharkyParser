using SharkyParser.Cli.Interfaces;
using SharkyParser.Core.Infrastructure;

namespace SharkyParser.Cli.Infrastructure;

/// <summary>
/// CLI application logger.
/// Inherits file-writing from FileAppLogger (Core) and adds CLI lifecycle
/// methods required by IAppLogger — keeping Core free of CLI concerns.
/// Writes to: {TempPath}/SharkyParser/Logs/AppLog.txt
/// </summary>
public class AppFileLogger : FileAppLogger, IAppLogger
{
    public AppFileLogger() : base("AppLog.txt") { }

    public void LogAppStart(string[] args)
        => WriteEntry("INFO", $"Application started with args: {string.Join(" ", args)}");

    public void LogModeDetected(string mode)
        => WriteEntry("INFO", $"Mode detected: {mode}");

    public void LogFileProcessed(string filePath)
        => WriteEntry("INFO", $"File processed: {filePath}");

    public void LogCommandExecution(string command, int exitCode)
        => WriteEntry("INFO", $"Command executed: {command} with exit code {exitCode}");
}
