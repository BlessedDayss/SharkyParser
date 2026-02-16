using SharkyParser.Core.Interfaces;

namespace SharkyParser.Core.Infrastructure;

/// <summary>
/// File-based IAppLogger implementation shared across all hosts (CLI, API, etc.).
/// Writes structured log entries to: {TempPath}/SharkyParser/Logs/{logFileName}
/// Thread-safe via lock for use in multi-threaded environments (e.g. web servers).
/// </summary>
public class FileAppLogger : IAppLogger
{
    private readonly string _logPath;
    private readonly object _lock = new();

    /// <summary>
    /// Creates a file logger that writes to {TempPath}/SharkyParser/Logs/{logFileName}.
    /// </summary>
    /// <param name="logFileName">Name of the log file, e.g. "AppLog.txt" or "ApiLog.txt".</param>
    public FileAppLogger(string logFileName = "AppLog.txt")
    {
        var logDir = Path.Combine(Path.GetTempPath(), "SharkyParser", "Logs");
        _logPath = Path.Combine(logDir, logFileName);

        Directory.CreateDirectory(logDir);
    }

    /// <summary>
    /// Creates a file logger with a fully custom path.
    /// </summary>
    /// <param name="logPath">Full absolute path to the log file.</param>
    /// <param name="isFullPath">Must be true to use this overload.</param>
    public FileAppLogger(string logPath, bool isFullPath)
    {
        _logPath = logPath;

        var directory = Path.GetDirectoryName(_logPath);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }
    }

    public string LogFilePath => _logPath;

    public void LogAppStart(string[] args)
        => WriteEntry("INFO", $"Application started with args: {string.Join(" ", args)}");

    public void LogModeDetected(string mode)
        => WriteEntry("INFO", $"Mode detected: {mode}");

    public void LogFileProcessed(string filePath)
        => WriteEntry("INFO", $"File processed: {filePath}");

    public void LogCommandExecution(string command, int exitCode)
        => WriteEntry("INFO", $"Command executed: {command} with exit code {exitCode}");

    public void LogError(string message, Exception? ex = null)
    {
        WriteEntry("ERROR", message);
        if (ex != null)
        {
            WriteEntry("ERROR", $"Exception: {ex.Message}\n{ex.StackTrace}");
        }
    }

    public void LogInfo(string message)
        => WriteEntry("INFO", message);

    private void WriteEntry(string level, string message)
    {
        var entry = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} [{level}] {message}{Environment.NewLine}";
        try
        {
            lock (_lock)
            {
                File.AppendAllText(_logPath, entry);
            }
        }
        catch
        {
            Console.Error.WriteLine($"Failed to write log: {entry.Trim()}");
        }
    }
}
