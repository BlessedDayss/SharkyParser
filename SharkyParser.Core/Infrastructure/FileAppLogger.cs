using SharkyParser.Core.Interfaces;

namespace SharkyParser.Core.Infrastructure;

/// <summary>
/// File-based ILogger implementation shared across all hosts (CLI, API, etc.).
/// Writes structured log entries to: {TempPath}/SharkyParser/Logs/{logFileName}
/// Thread-safe via lock for use in multi-threaded environments (e.g. web servers).
/// Host-specific lifecycle methods (LogAppStart, LogModeDetected, etc.) live in
/// the host's own logger class, not here — see IAppLogger in SharkyParser.Cli.
/// </summary>
public class FileAppLogger : ILogger
{
    private readonly string _logPath;
    private readonly object _lock = new();

    /// <summary>
    /// Creates a file logger that writes to {TempPath}/SharkyParser/Logs/{logFileName}.
    /// </summary>
    public FileAppLogger(string logFileName = "AppLog.txt")
    {
        var logDir = Path.Combine(Path.GetTempPath(), "SharkyParser", "Logs");
        _logPath = Path.Combine(logDir, logFileName);
        Directory.CreateDirectory(logDir);
    }

    /// <summary>
    /// Creates a file logger with a fully custom path.
    /// </summary>
    public FileAppLogger(string logPath, bool isFullPath)
    {
        _logPath = logPath;
        var directory = Path.GetDirectoryName(_logPath);
        if (!string.IsNullOrEmpty(directory))
            Directory.CreateDirectory(directory);
    }

    public string LogFilePath => _logPath;

    public void LogError(string message, Exception? ex = null)
    {
        WriteEntry("ERROR", message);
        if (ex != null)
            WriteEntry("ERROR", $"Exception: {ex.Message}\n{ex.StackTrace}");
    }

    public void LogInfo(string message)
        => WriteEntry("INFO", message);

    /// <summary>
    /// Protected so subclasses (CLI AppFileLogger, API ApiLogger) can reuse
    /// the same thread-safe write pipeline.
    /// </summary>
    protected void WriteEntry(string level, string message)
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
