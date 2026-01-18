namespace SharkyParser.Core.Interfaces;

public interface IAppLogger
{
    void LogAppStart(string[] args);
    void LogModeDetected(string mode);
    void LogFileProcessed(string filePath);
    void LogCommandExecution(string command, int exitCode);
    void LogError(string message, Exception? ex = null);
    void LogInfo(string message);
}