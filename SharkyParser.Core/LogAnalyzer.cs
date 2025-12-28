namespace SharkyParser.Core;

/// <summary>
/// Analyzes log entries for errors and health status.
/// </summary>
public class LogAnalyzer
{
    public bool HasErrors(IEnumerable<LogEntry> entries)
        => entries.Any(e => e.Level.Contains("ERROR") || e.Level.Contains("FATAL"));

    public bool HasWarnings(IEnumerable<LogEntry> entries)
        => entries.Any(e => e.Level.Contains("WARN"));

    /// <summary>
    /// Gets statistics for a collection of log entries.
    /// </summary>
    public LogStatistics GetStatistics(IEnumerable<LogEntry> entries)
    {
        var list = entries.ToList();
        var errorCount = list.Count(e => e.Level.Contains("ERROR") || e.Level.Contains("FATAL"));
        
        return new LogStatistics(
            TotalCount: list.Count,
            ErrorCount: errorCount,
            WarningCount: list.Count(e => e.Level.Contains("WARN")),
            InfoCount: list.Count(e => e.Level.Contains("INFO")),
            IsHealthy: errorCount == 0
        );
    }
}
