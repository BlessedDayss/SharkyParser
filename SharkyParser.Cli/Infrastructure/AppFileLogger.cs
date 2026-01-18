using SharkyParser.Core.Interfaces;

namespace SharkyParser.Cli.Infrastructure;

public class AppFileLogger : IAppLogger
{
    private readonly string _logPath;
    
    public AppFileLogger(string logPath = "Logs/AppLog.txt")
    {
        _logPath = logPath;
        Directory.CreateDirectory(Path.GetDirectoryName(_logPath));
    }

    public void LogAppStart(string[] args)
        => Log("INFO", $"Application started with args: {string.Join(" ", args)}");
    public void LogModeDetected(string mode) => Log("INFO", $"Mode detected: {mode}");
    public void LogFileProcessed(string filePath) => Log("INFO", $"File processed: {filePath}");
    public void LogCommandExecution(string command, int exitCode) => Log("INFO", $"Command executed: {command} with exit code {exitCode}");
    public void LogError(string message, Exception? ex = null)
    {
        Log("ERROR", message);
        if (ex != null) Log("ERROR", $"Exception: {ex.Message}\n{ex.StackTrace}");
    }
    public void LogInfo(string message) => Log("INFO", message);

    private void Log(string level, string message)
    {
        var entry = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} [{level}] {message}{Environment.NewLine}";
        try
        {
            File.AppendAllText(_logPath, entry);
        }
        catch
        {
            Console.Error.WriteLine($"Failed to log: {entry.Trim()}");
        }
    }
}